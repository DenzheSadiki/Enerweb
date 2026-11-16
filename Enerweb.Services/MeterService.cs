using Enerweb.Domain.Models.Dtos;
using Enerweb.Domain.Models;
using Enerweb.Interfaces;
using Enerweb.Interfaces.Repositories;
using Enerweb.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace Enerweb.Services;

public sealed class MeterService : BaseService<Meters>, IMeterService
{
    private readonly ISiteRepository sites;
    private readonly IReadingRepository readings;

    public MeterService(IMeterRepository meters, ISiteRepository sites,
        IReadingRepository readings, IUnitOfWork unitOfWork) : base(meters, unitOfWork)
        => (this.sites, this.readings) = (sites, readings);

    protected override Guid GetId(Meters entity) => entity.Id;

    protected override async Task ValidateAsync(Meters entity)
    {
        RequireId(entity.SiteId);
        RequireText(entity.MeterNumber, nameof(entity.MeterNumber), 100);
        RequireText(entity.SerialNumber, nameof(entity.SerialNumber));
        RequireText(entity.MeterName, nameof(entity.MeterName));
        RequireText(entity.MeterType, nameof(entity.MeterType));
        RequireText(entity.MeterCategory, nameof(entity.MeterCategory));
        RequireText(entity.UnitOfMeasurement, nameof(entity.UnitOfMeasurement));
        RequireText(entity.PropertyId, nameof(entity.PropertyId));
        RequireText(entity.AccountNumber, nameof(entity.AccountNumber));
        RequireText(entity.Status, nameof(entity.Status));
        if (entity.PreviousReading < 0 || entity.CurrentReading < entity.PreviousReading)
            throw new ArgumentException("Current reading must be at least the nonnegative previous reading.");
        if (!await sites.Query().AnyAsync(site => site.Id == entity.SiteId))
            throw new KeyNotFoundException("The meter's site does not exist.");
        if (await Repository.Query().AnyAsync(meter => meter.MeterNumber == entity.MeterNumber && meter.Id != entity.Id))
            throw new InvalidOperationException("Meter number must be unique.");
    }

    public override Task CreateAsync(Meters entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        entity.CreatedAt = DateTime.UtcNow;
        entity.Consumption = entity.CurrentReading - entity.PreviousReading;
        return base.CreateAsync(entity);
    }

    protected override void ApplyChanges(Meters source, Meters target)
    {
        target.SiteId = source.SiteId;
        target.MeterNumber = source.MeterNumber;
        target.SerialNumber = source.SerialNumber;
        target.MeterName = source.MeterName;
        target.MeterType = source.MeterType;
        target.MeterCategory = source.MeterCategory;
        target.UnitOfMeasurement = source.UnitOfMeasurement;
        target.CurrentReading = source.CurrentReading;
        target.PreviousReading = source.PreviousReading;
        target.Consumption = source.CurrentReading - source.PreviousReading;
        target.InstallationDate = source.InstallationDate;
        target.LastReadingDate = source.LastReadingDate;
        target.PropertyId = source.PropertyId;
        target.AccountNumber = source.AccountNumber;
        target.IsActive = source.IsActive;
        target.Status = source.Status;
        target.UpdatedAt = DateTime.UtcNow;
    }

    protected override async Task ValidateDeleteAsync(Meters entity)
    {
        if (await readings.Query().AnyAsync(reading => reading.MeterId == entity.Id))
            throw new InvalidOperationException("Remove the meter's readings before deleting the meter.");
    }

    public Task<List<Meters>> GetMetersBySiteAsync(Guid siteId)
    {
        RequireId(siteId);
        return Repository.Query().Where(meter => meter.SiteId == siteId)
            .OrderBy(meter => meter.MeterNumber).ToListAsync();
    }

    public async Task<MeterWithReadings?> GetMeterWithReadingsAsync(Guid meterId)
    {
        RequireId(meterId);
        var meter = await Repository.Query().SingleOrDefaultAsync(meter => meter.Id == meterId);
        if (meter == null) return null;
        var items = await readings.Query().Where(reading => reading.MeterId == meterId)
            .OrderByDescending(reading => reading.Timestamp).ToListAsync();
        return new MeterWithReadings(meter, items);
    }
}
