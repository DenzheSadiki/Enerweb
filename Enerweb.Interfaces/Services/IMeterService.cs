using Enerweb.Domain.Models.Dtos;
using Enerweb.Domain.Models;

namespace Enerweb.Interfaces.Services;

public interface IMeterService : IBaseService<Meters>
{
    Task<List<Meters>> GetMetersBySiteAsync(Guid siteId);
    Task<MeterWithReadings?> GetMeterWithReadingsAsync(Guid meterId);
}
