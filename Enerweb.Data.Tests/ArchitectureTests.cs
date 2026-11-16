using Enerweb.Domain.Models.Dtos;
using Enerweb.Data;
using Enerweb.Domain.Models;
using Enerweb.Interfaces;
using Enerweb.Interfaces.Repositories;
using Enerweb.Interfaces.Services;
using Enerweb.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Enerweb.Data.Tests;

public sealed class ArchitectureTests
{
    [Fact]
    public async Task LogServicePersistsSuccessfulRequestsWithoutAnErrorRecord()
    {
        using var app = new TestScope();
        var log = new DatabaseLog { TraceId = "request-test", RequestPath = "/test", HttpMethod = "GET", StatusCode = 200, DurationMilliseconds = 12 };
        await app.Get<ILogService>().LogRequestAsync(log);
        app.Context.ChangeTracker.Clear();
        var saved = await app.Context.DatabaseLogs.SingleAsync();
        Assert.Null(saved.ErrorId);
        Assert.Equal("Information", saved.Level);
        Assert.Equal(DateTimeKind.Utc, saved.Timestamp.Kind);
        Assert.Equal(12, saved.DurationMilliseconds);
        Assert.Empty(await app.Context.Errors.ToListAsync());
    }

    [Fact]
    public async Task LogServicePersistsBothRecordsWithUtcTimestamp()
    {
        using var app = new TestScope();
        var error = new Error { ExceptionType = "TestException", Message = "Test error" };
        var log = new DatabaseLog { TraceId = "data-test", RequestPath = "/test", HttpMethod = "POST", StatusCode = 500 };
        await app.Get<ILogService>().LogErrorAsync(error, log);
        app.Context.ChangeTracker.Clear();
        var saved = await app.Context.DatabaseLogs.SingleAsync();
        Assert.Equal(error.Id, saved.ErrorId);
        Assert.Equal(DateTimeKind.Utc, saved.Timestamp.Kind);
        Assert.Equal("Error", saved.Level);
        Assert.Equal(error.Id, (await app.Context.Errors.SingleAsync()).Id);
    }

    [Fact]
    public async Task DailyBreakdownUsesOnlyInRangePairsAndLaterUtcDateIgnoringNonpositiveDeltas()
    {
        using var app = new TestScope();
        var from = new DateTime(2020, 1, 1, 23, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2020, 1, 5, 0, 0, 0, DateTimeKind.Utc);
        var samples = new[]
        {
            (from.AddMinutes(-1), 1000m), // Must not form a pair across the lower bound.
            (from, 100m),
            (new DateTime(2020, 1, 2, 0, 0, 0, DateTimeKind.Utc), 130m),
            (new DateTime(2020, 1, 2, 4, 0, 0, DateTimeKind.Utc), 120m),
            (new DateTime(2020, 1, 3, 1, 0, 0, DateTimeKind.Utc), 150m),
            (new DateTime(2020, 1, 3, 3, 0, 0, DateTimeKind.Utc), 150m),
            (to, 50m),
            (to.AddMinutes(1), 9999m) // Must not form a pair across the upper bound.
        };
        // Insert out of order to verify summary ordering is by timestamp.
        app.Context.Readings.AddRange(samples.Reverse().Select(sample => new Reading
        {
            Id = Guid.NewGuid(), MeterId = DemoMeter, Timestamp = sample.Item1, Value = sample.Item2
        }));
        var otherMeter = NewMeter(DemoSite);
        app.Context.Meters.Add(otherMeter);
        app.Context.Readings.Add(new Reading
        {
            Id = Guid.NewGuid(), MeterId = otherMeter.Id, Timestamp = from.AddHours(2), Value = 50000m
        });
        await app.Context.SaveChangesAsync();

        var summary = await app.Get<IReadingService>().GetSummaryAsync(new ReadingSummaryRequest
        {
            MeterId = DemoMeter, FromUtc = from, ToUtc = to
        });
        Assert.Equal(6, summary.ReadingCount);
        Assert.Equal(from, summary.FirstTimestamp);
        Assert.Equal(to, summary.LastTimestamp);
        Assert.Equal(-50m, summary.Consumption); // Overall delta is not clamped or daily totals summed.
        Assert.Collection(summary.DailyBreakdown,
            day => { Assert.Equal(new DateOnly(2020, 1, 2), day.Date); Assert.Equal(30m, day.Consumption); },
            day => { Assert.Equal(new DateOnly(2020, 1, 3), day.Date); Assert.Equal(30m, day.Consumption); },
            day => { Assert.Equal(new DateOnly(2020, 1, 5), day.Date); Assert.Equal(0m, day.Consumption); });
    }

    [Fact]
    public async Task ReadingSummaryFiltersInclusivelyAndCalculatesConsumption()
    {
        using var app = new TestScope();
        var service = app.Get<IReadingService>();
        var summary = await service.GetSummaryAsync(new ReadingSummaryRequest { MeterId = DemoMeter, FromUtc = DateTime.UnixEpoch, ToUtc = DateTime.UtcNow });
        Assert.Equal(10, summary.ReadingCount);
        Assert.Equal(330m, summary.Consumption);
        Assert.Equal(1250m, summary.MinimumValue);
        Assert.Equal(1580m, summary.MaximumValue);
        var filtered = await service.GetSummaryAsync(new ReadingSummaryRequest
        {
            MeterId = DemoMeter,
            FromUtc = new DateTime(2026, 11, 15, 2, 0, 0, DateTimeKind.Utc),
            ToUtc = new DateTime(2026, 11, 15, 5, 0, 0, DateTimeKind.Utc)
        });
        Assert.Equal(4, filtered.ReadingCount);
        Assert.Equal(98m, filtered.Consumption);
        Assert.Equal(1361.5m, filtered.AverageValue);
    }

    [Fact]
    public async Task ReadingSummaryHandlesEmptySingleAndInvalidRequests()
    {
        using var app = new TestScope();
        var service = app.Get<IReadingService>();
        var date = new DateTime(2026, 11, 15, 0, 0, 0, DateTimeKind.Utc);
        var single = await service.GetSummaryAsync(new ReadingSummaryRequest { MeterId = DemoMeter, FromUtc = date, ToUtc = date });
        Assert.Equal(1, single.ReadingCount);
        Assert.Equal(0m, single.Consumption);
        Assert.Empty(single.DailyBreakdown);
        var empty = await service.GetSummaryAsync(new ReadingSummaryRequest { MeterId = DemoMeter, FromUtc = date.AddDays(-2), ToUtc = date.AddDays(-1) });
        Assert.Equal(0, empty.ReadingCount);
        Assert.Equal(0m, empty.Consumption);
        Assert.Null(empty.AverageValue);
        Assert.Null(empty.FirstTimestamp);
        Assert.Empty(empty.DailyBreakdown);
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.GetSummaryAsync(null!));
        await Assert.ThrowsAsync<ArgumentException>(() => service.GetSummaryAsync(new ReadingSummaryRequest()));
        await Assert.ThrowsAsync<ArgumentException>(() => service.GetSummaryAsync(new ReadingSummaryRequest { MeterId = DemoMeter, FromUtc = date, ToUtc = date.AddDays(-1) }));
        await Assert.ThrowsAsync<ArgumentException>(() => service.GetSummaryAsync(new ReadingSummaryRequest { MeterId = DemoMeter, FromUtc = date }));
        await Assert.ThrowsAsync<ArgumentException>(() => service.GetSummaryAsync(new ReadingSummaryRequest { MeterId = DemoMeter, ToUtc = date }));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetSummaryAsync(new ReadingSummaryRequest { MeterId = Guid.NewGuid(), FromUtc = date, ToUtc = date }));
    }

    [Fact]
    public async Task SeededReadingsIncreaseHourlyAndMatchMeterSummary()
    {
        using var app = new TestScope();
        var readings = await app.Context.Readings.Where(x => x.MeterId == DemoMeter)
            .OrderBy(x => x.Timestamp).ToListAsync();
        Assert.Equal(10, readings.Count);
        for (var index = 1; index < readings.Count; index++)
        {
            Assert.Equal(TimeSpan.FromHours(1), readings[index].Timestamp - readings[index - 1].Timestamp);
            Assert.True(readings[index].Value > readings[index - 1].Value);
            Assert.Equal(DateTimeKind.Utc, readings[index].Timestamp.Kind);
        }
        var meter = await app.Context.Meters.SingleAsync(x => x.Id == DemoMeter);
        Assert.Equal(readings[^1].Value, meter.CurrentReading);
        Assert.Equal(readings[^2].Value, meter.PreviousReading);
        Assert.Equal(meter.CurrentReading - meter.PreviousReading, meter.Consumption);
        Assert.Equal(readings[^1].Timestamp, meter.LastReadingDate);
    }

    [Fact]
    public async Task DatabaseLoadsRequiredAndNullableTimestampsAsUtc()
    {
        using var app = new TestScope();
        var meter = await app.Context.Meters.FirstAsync();
        meter.InstallationDate = new DateTime(2020, 1, 2, 10, 30, 0, DateTimeKind.Unspecified);
        meter.LastReadingDate = new DateTime(2020, 1, 3, 10, 30, 0, DateTimeKind.Local);
        var expectedLastReading = meter.LastReadingDate.Value.ToUniversalTime();
        await app.Context.SaveChangesAsync();
        app.Context.ChangeTracker.Clear();
        meter = await app.Context.Meters.FirstAsync();
        Assert.Equal(DateTimeKind.Utc, meter.CreatedAt.Kind);
        Assert.Equal(DateTimeKind.Utc, meter.InstallationDate!.Value.Kind);
        Assert.Equal(DateTimeKind.Utc, meter.LastReadingDate!.Value.Kind);
        Assert.Equal(expectedLastReading, meter.LastReadingDate.Value);
        Assert.Null(meter.UpdatedAt);
    }

    private static readonly Guid DemoSite = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid DemoMeter = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public async Task RepositoryStagesCrudUntilExplicitCommit()
    {
        using var app = new TestScope();
        var repository = app.Get<IBaseRepository<Site>>();
        var work = app.Get<IUnitOfWork>();
        var site = NewSite();
        await repository.AddAsync(site);
        Assert.False(await repository.Query().AnyAsync(x => x.Id == site.Id));
        await work.SaveChangesAsync();
        app.Context.ChangeTracker.Clear();
        var found = await repository.GetByIdAsync(site.Id);
        Assert.NotNull(found);
        found.SiteName = "Updated";
        repository.Update(found);
        Assert.NotEqual("Updated", (await repository.Query().SingleAsync(x => x.Id == site.Id)).SiteName);
        await work.SaveChangesAsync();
        Assert.Equal("Updated", (await repository.Query().SingleAsync(x => x.Id == site.Id)).SiteName);
        repository.Delete(found);
        Assert.True(await repository.Query().AnyAsync(x => x.Id == site.Id));
        await work.SaveChangesAsync();
        Assert.Null(await repository.GetByIdAsync(site.Id));
        Assert.Null(await repository.GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task QueriesAreDeferredAndSupportFilteringProjectionSortingAndPagination()
    {
        using var app = new TestScope();
        var repository = app.Get<IBaseRepository<Site>>();
        var query = repository.Query().Where(x => x.SiteCode.StartsWith("QUERY"))
            .OrderByDescending(x => x.SiteName).Select(x => x.SiteName).Skip(1).Take(1);
        await repository.AddAsync(NewSite("QUERY-A", "Alpha"));
        await repository.AddAsync(NewSite("QUERY-B", "Beta"));
        await app.Get<IUnitOfWork>().SaveChangesAsync();
        Assert.Equal(new[] { "Alpha" }, await query.ToListAsync());
    }

    [Fact]
    public async Task QueriesSupportIncludesGroupingAndAggregates()
    {
        using var app = new TestScope();
        var site = await app.Get<IBaseRepository<Site>>().Query().Include(x => x.Meters)
            .SingleAsync(x => x.Id == DemoSite);
        Assert.Single(site.Meters);
        var totals = await app.Get<IBaseRepository<Reading>>().Query()
            .Where(x => x.Meter.SiteId == DemoSite).GroupBy(x => x.MeterId)
            .Select(group => new { MeterId = group.Key, Count = group.Count() }).ToListAsync();
        Assert.Equal(DemoMeter, Assert.Single(totals).MeterId);
        Assert.Equal(10, totals[0].Count);
    }

    [Fact]
    public async Task TrackingIsExplicitForQueries()
    {
        using var app = new TestScope();
        var repository = app.Get<IBaseRepository<Site>>();
        (await repository.Query().FirstAsync()).SiteName = "Not saved";
        Assert.Empty(app.Context.ChangeTracker.Entries());
        await app.Get<IUnitOfWork>().SaveChangesAsync();
        Assert.DoesNotContain(await repository.Query().Select(x => x.SiteName).ToListAsync(), x => x == "Not saved");
        (await repository.QueryTracking().FirstAsync()).SiteName = "Tracked";
        await app.Get<IUnitOfWork>().SaveChangesAsync();
        Assert.Contains(await repository.Query().Select(x => x.SiteName).ToListAsync(), x => x == "Tracked");
    }

    [Fact]
    public async Task UnitOfWorkCommitsMultipleRepositoriesTogether()
    {
        using var app = new TestScope();
        var site = NewSite();
        var meter = NewMeter(site.Id);
        await app.Get<IBaseRepository<Site>>().AddAsync(site);
        await app.Get<IBaseRepository<Meters>>().AddAsync(meter);
        Assert.Equal(2, await app.Get<IUnitOfWork>().SaveChangesAsync());
        Assert.True(await app.Context.Meters.AsNoTracking().AnyAsync(x => x.SiteId == site.Id));
    }

    [Fact]
    public async Task FailedUnitOfWorkRollsBackAllChanges()
    {
        using var app = new TestScope();
        var site = NewSite();
        await app.Get<IBaseRepository<Site>>().AddAsync(site);
        await app.Get<IBaseRepository<Reading>>().AddAsync(new Reading
        {
            Id = Guid.NewGuid(), MeterId = Guid.NewGuid(), Timestamp = DateTime.UtcNow, Value = 1
        });
        await Assert.ThrowsAsync<DbUpdateException>(() => app.Get<IUnitOfWork>().SaveChangesAsync());
        app.Context.ChangeTracker.Clear();
        Assert.False(await app.Context.Sites.AnyAsync(x => x.Id == site.Id));
    }

    [Fact]
    public void ServicesAndRepositoriesHaveScopedLifetimes()
    {
        using var app = new TestScope();
        Assert.IsType<SiteService>(app.Get<ISiteService>());
        Assert.IsType<MeterService>(app.Get<IMeterService>());
        Assert.IsType<ReadingService>(app.Get<IReadingService>());
        Assert.Same(app.Get<ISiteService>(), app.Get<ISiteService>());
        Assert.Same(app.Get<IUnitOfWork>(), app.Get<IUnitOfWork>());
        Assert.Same(app.Get<IBaseRepository<Site>>(), app.Get<IBaseRepository<Site>>());
        Assert.Same(app.Get<ISiteRepository>(), app.Get<IBaseRepository<Site>>());
        Assert.Same(app.Get<IMeterRepository>(), app.Get<IBaseRepository<Meters>>());
        Assert.Same(app.Get<IReadingRepository>(), app.Get<IBaseRepository<Reading>>());
        Assert.Same(app.Get<IAddressRepository>(), app.Get<IBaseRepository<Address>>());
        Assert.Same(app.Get<IContactPersonRepository>(), app.Get<IBaseRepository<ContactPerson>>());
        Assert.IsType<AddressService>(app.Get<IAddressService>());
        Assert.IsType<ContactPersonService>(app.Get<IContactPersonService>());
        using var other = app.Provider.CreateScope();
        Assert.NotSame(app.Context, other.ServiceProvider.GetRequiredService<EnerwebDbContext>());
        Assert.NotSame(app.Get<ISiteService>(), other.ServiceProvider.GetRequiredService<ISiteService>());
    }

    [Fact]
    public async Task SiteServiceValidatesRequiredAndUniqueCodes()
    {
        using var app = new TestScope();
        var service = app.Get<ISiteService>();
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(NewSite("", "")));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(NewSite("DEMO-001")));
        Assert.Single(await service.GetAllAsync());
    }

    [Fact]
    public async Task SiteServiceUpdatesDetachedInputAndPreservesAuditFields()
    {
        using var app = new TestScope();
        var service = app.Get<ISiteService>();
        var site = NewSite();
        await service.CreateAsync(site);
        var createdAt = site.CreatedAt;
        await service.UpdateAsync(new Site { Id = site.Id, SiteCode = site.SiteCode, SiteName = "Changed", IsActive = true });
        var updated = await service.GetByIdAsync(site.Id);
        Assert.Equal("Changed", updated!.SiteName);
        Assert.Equal(createdAt, updated.CreatedAt);
        Assert.NotNull(updated.UpdatedAt);
        await service.DeleteAsync(new Site { Id = site.Id });
        Assert.Null(await service.GetByIdAsync(site.Id));
    }

    [Fact]
    public async Task MissingUpdatesAndDeletesAreReported()
    {
        using var app = new TestScope();
        var service = app.Get<ISiteService>();
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.UpdateAsync(NewSite()));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.DeleteAsync(NewSite()));
    }

    [Fact]
    public async Task SiteAndMeterDeletionGuardExistingChildren()
    {
        using var app = new TestScope();
        await Assert.ThrowsAsync<InvalidOperationException>(() => app.Get<ISiteService>().DeleteAsync(new Site { Id = DemoSite }));
        await Assert.ThrowsAsync<InvalidOperationException>(() => app.Get<IMeterService>().DeleteAsync(new Meters { Id = DemoMeter }));
    }

    [Fact]
    public async Task SpecialisedServiceQueriesLoadRelatedEntities()
    {
        using var app = new TestScope();
        var sites = app.Get<ISiteService>();
        Assert.Single((await sites.GetSiteWithMetersAsync(DemoSite))!.Meters);
        Assert.Single(await sites.GetSitesByNameAsync("Demo"));
        var meters = app.Get<IMeterService>();
        Assert.Single(await meters.GetMetersBySiteAsync(DemoSite));
        Assert.Equal(10, (await meters.GetMeterWithReadingsAsync(DemoMeter))!.Readings.Count);
        Assert.Null(await meters.GetMeterWithReadingsAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task MeterServiceValidatesParentNumbersAndReadings()
    {
        using var app = new TestScope();
        var service = app.Get<IMeterService>();
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateAsync(NewMeter(Guid.NewGuid())));
        var duplicate = NewMeter(DemoSite);
        duplicate.MeterNumber = "DEMO-MTR-001";
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(duplicate));
        var invalid = NewMeter(DemoSite);
        invalid.PreviousReading = 100;
        invalid.CurrentReading = 50;
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(invalid));
        var meter = NewMeter(DemoSite);
        meter.CurrentReading = 120;
        meter.PreviousReading = 100;
        meter.Consumption = 999;
        await service.CreateAsync(meter);
        Assert.Equal(20m, (await service.GetByIdAsync(meter.Id))!.Consumption);
    }

    [Fact]
    public async Task ReadingServiceValidatesValuesDatesAndParent()
    {
        using var app = new TestScope();
        var service = app.Get<IReadingService>();
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(new Reading { MeterId = DemoMeter, Value = -1, Timestamp = DateTime.UtcNow }));
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(new Reading { MeterId = DemoMeter, Value = 1, Timestamp = DateTime.UtcNow.AddDays(1) }));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateAsync(new Reading { MeterId = Guid.NewGuid(), Value = 1, Timestamp = DateTime.UtcNow }));
        await Assert.ThrowsAsync<ArgumentException>(() => service.GetByMeterAsync(DemoMeter, DateTime.UtcNow, DateTime.UtcNow.AddDays(-1)));
    }

    [Fact]
    public async Task ReadingServiceSupportsCrudAndDateFiltering()
    {
        using var app = new TestScope();
        var service = app.Get<IReadingService>();
        var time = DateTime.UtcNow.AddHours(-1);
        var reading = new Reading { MeterId = DemoMeter, Value = 1300, Timestamp = time };
        await service.CreateAsync(reading);
        Assert.NotEqual(Guid.Empty, reading.Id);
        await service.UpdateAsync(new Reading { Id = reading.Id, MeterId = DemoMeter, Value = 1301, Timestamp = time });
        Assert.Equal(1301m, (await service.GetByIdAsync(reading.Id))!.Value);
        var results = await service.GetByMeterAsync(DemoMeter, time.AddMinutes(-1), time.AddMinutes(1));
        Assert.Contains(results, x => x.Id == reading.Id);
        await service.DeleteAsync(new Reading { Id = reading.Id });
        Assert.Null(await service.GetByIdAsync(reading.Id));
    }

    [Fact]
    public void SqlServerModelStillMatchesTheExistingMigration()
    {
        using var context = new EnerwebDbContext(new DbContextOptionsBuilder<EnerwebDbContext>()
            .UseSqlServer("Server=localhost;Database=ModelOnly;Integrated Security=True;TrustServerCertificate=True").Options);
        Assert.False(context.Database.HasPendingModelChanges());
    }

    private static Site NewSite(string? code = null, string name = "Test Site") => new()
    {
        Id = Guid.NewGuid(), SiteCode = code ?? Guid.NewGuid().ToString("N"),
        SiteName = name, IsActive = true, CreatedAt = DateTime.UtcNow
    };

    private static Meters NewMeter(Guid siteId) => new()
    {
        Id = Guid.NewGuid(), SiteId = siteId, MeterNumber = Guid.NewGuid().ToString("N"),
        SerialNumber = "Serial", MeterName = "Test Meter", MeterType = "Electricity",
        MeterCategory = "Postpaid", UnitOfMeasurement = "kWh", PropertyId = "Property",
        AccountNumber = "Account", Status = "Active", IsActive = true, CreatedAt = DateTime.UtcNow
    };

    private sealed class TestScope : IDisposable
    {
        private readonly SqliteConnection connection = new("Data Source=:memory:;Foreign Keys=True");
        private readonly IServiceScope scope;
        public ServiceProvider Provider { get; }
        public EnerwebDbContext Context => Get<EnerwebDbContext>();

        public TestScope()
        {
            connection.Open();
            var services = new ServiceCollection();
            services.AddEnerwebData(options => options.UseSqlite(connection));
            services.AddEnerwebServices();
            Provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
            scope = Provider.CreateScope();
            Context.Database.EnsureCreated();
        }

        public T Get<T>() where T : notnull => scope.ServiceProvider.GetRequiredService<T>();
        public void Dispose()
        {
            scope.Dispose();
            Provider.Dispose();
            connection.Dispose();
        }
    }
}
