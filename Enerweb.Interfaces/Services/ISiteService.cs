using Enerweb.Domain.Models;

namespace Enerweb.Interfaces.Services;

public interface ISiteService : IBaseService<Site>
{
    Task<Site?> GetSiteWithMetersAsync(Guid siteId);
    Task<List<Site>> GetSitesByNameAsync(string name);
}
