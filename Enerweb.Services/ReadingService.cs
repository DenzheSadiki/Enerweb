using Enerweb.Domain.Models.Dtos;
using Enerweb.Domain.Models;
using Enerweb.Interfaces;
using Enerweb.Interfaces.Repositories;
using Enerweb.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace Enerweb.Services;

public sealed class ReadingService : BaseService<Reading>, IReadingService
{
    private readonly IMeterRepository meters;

    public ReadingService(IReadingRepository readings, IMeterRepository meters,
        IUnitOfWork unitOfWork) : base(readings, unitOfWork) => this.meters = meters;

    protected override Guid GetId(Reading entity) => entity.Id;

    public async Task<ReadingSummaryDto> GetSummaryAsync(ReadingSummaryRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        RequireId(request.MeterId);
        if (!request.FromUtc.HasValue || !request.ToUtc.HasValue)
            throw new ArgumentException("Both fromUtc and toUtc are required.");
        var start = AsUtc(request.FromUtc);
        var end = AsUtc(request.ToUtc);
        if (start.HasValue && end.HasValue && start > end)
            throw new ArgumentException("fromUtc must not be after toUtc.");
        if (!await meters.Query().AnyAsync(meter => meter.Id == request.MeterId))
            throw new KeyNotFoundException("The meter does not exist.");

        var query = Repository.Query().Where(reading => reading.MeterId == request.MeterId);
        if (start.HasValue) query = query.Where(reading => reading.Timestamp >= start.Value);
        if (end.HasValue) query = query.Where(reading => reading.Timestamp <= end.Value);
        var readings = await query.OrderBy(reading => reading.Timestamp).ThenBy(reading => reading.Id)
            .Select(reading => new { reading.Timestamp, reading.Value }).ToListAsync();
        if (readings.Count == 0)
            return new ReadingSummaryDto { MeterId = request.MeterId };

        var dailyTotals = new SortedDictionary<DateOnly, decimal>();
        for (var index = 1; index < readings.Count; index++)
        {
            var day = DateOnly.FromDateTime(readings[index].Timestamp);
            var difference = readings[index].Value - readings[index - 1].Value;
            dailyTotals.TryGetValue(day, out var total);
            dailyTotals[day] = total + Math.Max(0m, difference);
        }

        return new ReadingSummaryDto
        {
            MeterId = request.MeterId,
            ReadingCount = readings.Count,
            FirstTimestamp = readings[0].Timestamp,
            LastTimestamp = readings[^1].Timestamp,
            FirstValue = readings[0].Value,
            LastValue = readings[^1].Value,
            MinimumValue = readings.Min(reading => reading.Value),
            MaximumValue = readings.Max(reading => reading.Value),
            AverageValue = readings.Average(reading => reading.Value),
            Consumption = readings.Count < 2 ? 0m : readings[^1].Value - readings[0].Value,
            DailyBreakdown = dailyTotals.Select(day => new DailyConsumptionDto
            {
                Date = day.Key,
                Consumption = day.Value
            }).ToList()
        };
    }

    private static DateTime? AsUtc(DateTime? value) => value.HasValue
        ? value.Value.Kind == DateTimeKind.Local ? value.Value.ToUniversalTime()
            : DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
        : null;

    protected override async Task ValidateAsync(Reading entity)
    {
        RequireId(entity.MeterId);
        entity.Timestamp = AsUtc(entity.Timestamp)!.Value;
        if (entity.Value < 0) throw new ArgumentException("Reading value cannot be negative.");
        if (entity.Timestamp == default || entity.Timestamp > DateTime.UtcNow)
            throw new ArgumentException("A reading timestamp is required and cannot be in the future.");
        if (!await meters.Query().AnyAsync(meter => meter.Id == entity.MeterId))
            throw new KeyNotFoundException("The reading's meter does not exist.");
        if (await Repository.Query().AnyAsync(reading => reading.MeterId == entity.MeterId
            && reading.Timestamp == entity.Timestamp && reading.Id != entity.Id))
            throw new InvalidOperationException("A reading already exists for this meter at this UTC timestamp.");
    }

    public override Task CreateAsync(Reading entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        if (entity.Meter != null)
            throw new ArgumentException("Supply MeterId rather than a meter object when creating a reading.");
        return base.CreateAsync(entity);
    }

    protected override void ApplyChanges(Reading source, Reading target)
    {
        target.MeterId = source.MeterId;
        target.Value = source.Value;
        target.Timestamp = source.Timestamp;
    }

    public Task<List<Reading>> GetByMeterAsync(Guid meterId, DateTime? startDate = null, DateTime? endDate = null)
    {
        RequireId(meterId);
        startDate = AsUtc(startDate);
        endDate = AsUtc(endDate);
        if (startDate.HasValue && endDate.HasValue && startDate > endDate)
            throw new ArgumentException("Start date must not be after end date.");
        var query = Repository.Query().Where(reading => reading.MeterId == meterId);
        if (startDate.HasValue) query = query.Where(reading => reading.Timestamp >= startDate.Value);
        if (endDate.HasValue) query = query.Where(reading => reading.Timestamp <= endDate.Value);
        return query.OrderByDescending(reading => reading.Timestamp).ThenByDescending(reading => reading.Id).ToListAsync();
    }
}
