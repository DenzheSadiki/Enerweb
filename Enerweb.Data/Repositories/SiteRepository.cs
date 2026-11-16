using Enerweb.Domain.Models;
using Enerweb.Interfaces.Repositories;

namespace Enerweb.Data.Repositories;

public sealed class SiteRepository : BaseRepository<Site>, ISiteRepository
{
    public SiteRepository(EnerwebDbContext context) : base(context) { }
}
