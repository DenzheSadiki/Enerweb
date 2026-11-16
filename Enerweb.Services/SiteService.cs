using Enerweb.Domain.Models;
using Enerweb.Interfaces;
using Enerweb.Interfaces.Repositories;
using Enerweb.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace Enerweb.Services;

public sealed class SiteService : BaseService<Site>, ISiteService
{
    private readonly IMeterRepository meters;

    public SiteService(ISiteRepository sites, IMeterRepository meters, IUnitOfWork unitOfWork)
        : base(sites, unitOfWork) => this.meters = meters;

    protected override Guid GetId(Site entity) => entity.Id;

    protected override async Task ValidateAsync(Site entity)
    {
        RequireText(entity.SiteName, nameof(entity.SiteName));
        RequireText(entity.SiteCode, nameof(entity.SiteCode), 50);
        if (await Repository.Query().AnyAsync(site => site.SiteCode == entity.SiteCode && site.Id != entity.Id))
            throw new InvalidOperationException("Site code must be unique.");
    }

    public override Task CreateAsync(Site entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        if (entity.Meters == null || entity.Meters.Count != 0)
            throw new ArgumentException("Create the site first, then manage meters through the meter service.");
        entity.CreatedAt = DateTime.UtcNow;
        return base.CreateAsync(entity);
    }

    protected override void ApplyChanges(Site source, Site target)
    {
        target.SiteName = source.SiteName;
        target.SiteCode = source.SiteCode;
        target.IsActive = source.IsActive;
        target.UpdatedAt = DateTime.UtcNow;
    }

    protected override async Task ValidateDeleteAsync(Site entity)
    {
        if (await meters.Query().AnyAsync(meter => meter.SiteId == entity.Id))
            throw new InvalidOperationException("Remove the site's meters before deleting the site.");
    }

    public Task<Site?> GetSiteWithMetersAsync(Guid siteId)
    {
        RequireId(siteId);
        return Repository.Query().Include(site => site.Meters).SingleOrDefaultAsync(site => site.Id == siteId);
    }

    public Task<List<Site>> GetSitesByNameAsync(string name)
    {
        RequireText(name, nameof(name));
        return Repository.Query().Where(site => site.SiteName.Contains(name))
            .OrderBy(site => site.SiteName).ToListAsync();
    }
}
