using Enerweb.Data;
using Enerweb.Domain.Models;
using Enerweb.Domain.Models.Dtos;
using Enerweb.Interfaces;
using Enerweb.Interfaces.Repositories;
using Enerweb.Interfaces.Services;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Enerweb.Data.IntegrationTests;

public sealed class ReadingBusinessRulesTests : IClassFixture<SqlServerFixture>
{
    private readonly SqlServerFixture fixture;
    public ReadingBusinessRulesTests(SqlServerFixture fixture) => this.fixture = fixture;
    private static DateTime Utc(int day, int hour = 0) => new(2020, 1, day, hour, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task SiteCanHaveMultipleMetersAndMeterCanHaveMultipleUtcCumulativeReadings()
    {
        using var scope = fixture.Provider.CreateScope();
        var (first, second) = await CreateMeters(scope.ServiceProvider);
        var service = scope.ServiceProvider.GetRequiredService<IReadingService>();
        await service.CreateAsync(NewReading(first.Id, Utc(1), 100.1250m));
        await service.CreateAsync(NewReading(first.Id, Utc(1, 1), 110.2500m));
        await service.CreateAsync(NewReading(first.Id, Utc(1, 2), 130.5000m));

        // Verify persisted relationships and types in a fresh context, not cached entities.
        using var verification = fixture.Provider.CreateScope();
        var context = verification.ServiceProvider.GetRequiredService<EnerwebDbContext>();
        var site = await context.Sites.Include(x => x.Meters).SingleAsync(x => x.Id == first.SiteId);
        Assert.Equal(2, site.Meters.Count);
        Assert.Contains(site.Meters, x => x.Id == second.Id);
        var readings = await context.Readings.Where(x => x.MeterId == first.Id).OrderBy(x => x.Timestamp).ToListAsync();
        Assert.Equal(new[] { 100.1250m, 110.2500m, 130.5000m }, readings.Select(x => x.Value));
        Assert.All(readings, x => Assert.Equal(DateTimeKind.Utc, x.Timestamp.Kind));
    }

    [Fact]
    public async Task SummaryUsesInclusiveBoundsAndPositivePairDeltasAcrossUtcDays()
    {
        using var scope = fixture.Provider.CreateScope();
        var (meter, other) = await CreateMeters(scope.ServiceProvider);
        var service = scope.ServiceProvider.GetRequiredService<IReadingService>();
        var samples = new[] { (Utc(1, 22), 999m), (Utc(1, 23), 100m), (Utc(2), 130m),
            (Utc(2, 1), 120m), (Utc(2, 2), 140m), (Utc(3), 150m), (Utc(3, 1), 150m),
            (Utc(3, 2), 50m), (Utc(3, 3), 999m) };
        foreach (var (timestamp, value) in samples.Reverse())
            await service.CreateAsync(NewReading(meter.Id, timestamp, value));
        await service.CreateAsync(NewReading(other.Id, Utc(2), 50000m));
        var summary = await service.GetSummaryAsync(new ReadingSummaryRequest
        {
            MeterId = meter.Id, FromUtc = Utc(1, 23), ToUtc = Utc(3, 2)
        });
        Assert.Equal(7, summary.ReadingCount);
        Assert.Equal(Utc(1, 23), summary.FirstTimestamp);
        Assert.Equal(Utc(3, 2), summary.LastTimestamp);
        Assert.Equal(-50m, summary.Consumption);
        Assert.Collection(summary.DailyBreakdown,
            day => { Assert.Equal(new DateOnly(2020, 1, 2), day.Date); Assert.Equal(50m, day.Consumption); },
            day => { Assert.Equal(new DateOnly(2020, 1, 3), day.Date); Assert.Equal(10m, day.Consumption); });
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public async Task FewerThanTwoReadingsHaveZeroConsumptionAndNoDailyPairs(int count)
    {
        using var scope = fixture.Provider.CreateScope();
        var (meter, _) = await CreateMeters(scope.ServiceProvider);
        var service = scope.ServiceProvider.GetRequiredService<IReadingService>();
        if (count == 1) await service.CreateAsync(NewReading(meter.Id, Utc(1), 123m));
        var summary = await service.GetSummaryAsync(new ReadingSummaryRequest { MeterId = meter.Id, FromUtc = Utc(1), ToUtc = Utc(2) });
        Assert.Equal(count, summary.ReadingCount);
        Assert.Equal(0m, summary.Consumption);
        Assert.Empty(summary.DailyBreakdown);
    }

    [Fact]
    public async Task ServiceRejectsDuplicateCreatesAndUpdatesButAllowsSameRecordAndDifferentMeters()
    {
        using var scope = fixture.Provider.CreateScope();
        var (firstMeter, secondMeter) = await CreateMeters(scope.ServiceProvider);
        var service = scope.ServiceProvider.GetRequiredService<IReadingService>();
        var first = NewReading(firstMeter.Id, Utc(1), 100m);
        await service.CreateAsync(first);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(NewReading(firstMeter.Id, Utc(1), 999m)));
        var second = NewReading(firstMeter.Id, Utc(2), 110m);
        await service.CreateAsync(second);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateAsync(new Reading
        {
            Id = second.Id, MeterId = firstMeter.Id, Timestamp = Utc(1), Value = 120m
        }));
        await service.UpdateAsync(new Reading { Id = first.Id, MeterId = firstMeter.Id, Timestamp = Utc(1), Value = 101m });
        await service.CreateAsync(NewReading(secondMeter.Id, Utc(1), 500m));
        Assert.Equal(2, (await service.GetByMeterAsync(firstMeter.Id)).Count);
        Assert.Single(await service.GetByMeterAsync(secondMeter.Id));
    }

    [Fact]
    public async Task SqlServerUniqueIndexRejectsDuplicateWritesThatBypassServices()
    {
        Guid meterId;
        using (var setup = fixture.Provider.CreateScope())
        {
            var (meter, _) = await CreateMeters(setup.ServiceProvider);
            meterId = meter.Id;
            await setup.ServiceProvider.GetRequiredService<IReadingService>().CreateAsync(NewReading(meterId, Utc(1), 100m));
        }
        using var scope = fixture.Provider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IReadingRepository>();
        await repository.AddAsync(NewReading(meterId, Utc(1), 999m));
        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync());
        var sqlException = Assert.IsType<SqlException>(exception.InnerException);
        Assert.Contains(sqlException.Number, new[] { 2601, 2627 });
        using var verification = fixture.Provider.CreateScope();
        Assert.Single(await verification.ServiceProvider.GetRequiredService<IReadingService>().GetByMeterAsync(meterId));
    }

    private static Reading NewReading(Guid meterId, DateTime timestamp, decimal value)
        => new() { MeterId = meterId, Timestamp = timestamp, Value = value };

    [Fact]
    public async Task ConcurrentDuplicateCommitsAllowExactlyOneReading()
    {
        Guid meterId;
        using (var setup = fixture.Provider.CreateScope())
            meterId = (await CreateMeters(setup.ServiceProvider)).First.Id;
        using var first = fixture.Provider.CreateScope();
        using var second = fixture.Provider.CreateScope();
        await first.ServiceProvider.GetRequiredService<IReadingRepository>().AddAsync(NewReading(meterId, Utc(1), 100m));
        await second.ServiceProvider.GetRequiredService<IReadingRepository>().AddAsync(NewReading(meterId, Utc(1), 200m));
        async Task<Exception?> Commit(IServiceProvider provider)
        {
            try { await provider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(); return null; }
            catch (DbUpdateException exception) { return exception; }
        }
        var results = await Task.WhenAll(Commit(first.ServiceProvider), Commit(second.ServiceProvider));
        Assert.Single(results, result => result == null);
        var rejected = Assert.IsType<DbUpdateException>(Assert.Single(results, result => result != null));
        Assert.Contains(Assert.IsType<SqlException>(rejected.InnerException).Number, new[] { 2601, 2627 });
        using var verification = fixture.Provider.CreateScope();
        Assert.Single(await verification.ServiceProvider.GetRequiredService<IReadingService>().GetByMeterAsync(meterId));
    }

    private static async Task<(Meters First, Meters Second)> CreateMeters(IServiceProvider provider)
    {
        var site = new Site { SiteName = "Integration site", SiteCode = Guid.NewGuid().ToString("N"), IsActive = true };
        await provider.GetRequiredService<ISiteService>().CreateAsync(site);
        Meters NewMeter() => new()
        {
            SiteId = site.Id, MeterNumber = Guid.NewGuid().ToString("N"), SerialNumber = "Integration",
            MeterName = "Electricity", MeterType = "Electricity", MeterCategory = "Postpaid",
            UnitOfMeasurement = "kWh", PropertyId = "Integration", AccountNumber = "Integration",
            IsActive = true, Status = "Active"
        };
        var first = NewMeter();
        var second = NewMeter();
        var service = provider.GetRequiredService<IMeterService>();
        await service.CreateAsync(first);
        await service.CreateAsync(second);
        return (first, second);
    }
}
