using Enerweb.Domain.Models.Dtos;
using Enerweb.Domain.Models;

namespace Enerweb.Interfaces.Services;

public interface IReadingService : IBaseService<Reading>
{
    Task<ReadingSummaryDto> GetSummaryAsync(ReadingSummaryRequest request);
    Task<List<Reading>> GetByMeterAsync(Guid meterId, DateTime? startDate = null, DateTime? endDate = null);
}
