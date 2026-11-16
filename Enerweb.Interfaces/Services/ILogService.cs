using Enerweb.Domain.Models;

namespace Enerweb.Interfaces.Services;

public interface ILogService
{
    Task LogRequestAsync(DatabaseLog log, CancellationToken cancellationToken = default);
    Task LogErrorAsync(Error error, DatabaseLog log, CancellationToken cancellationToken = default);
}
