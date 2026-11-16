using Enerweb.Domain.Models;
using Enerweb.Interfaces;
using Enerweb.Interfaces.Repositories;
using Enerweb.Interfaces.Services;

namespace Enerweb.Services;

public sealed class LogService : ILogService
{
    private readonly IErrorRepository errors;
    private readonly IDatabaseLogRepository logs;
    private readonly IUnitOfWork unitOfWork;

    public LogService(IErrorRepository errors, IDatabaseLogRepository logs, IUnitOfWork unitOfWork)
        => (this.errors, this.logs, this.unitOfWork) = (errors, logs, unitOfWork);

    public async Task LogErrorAsync(Error error, DatabaseLog log, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(error);
        ArgumentNullException.ThrowIfNull(log);
        if (string.IsNullOrWhiteSpace(error.ExceptionType))
            throw new ArgumentException("An exception type is required.", nameof(error));
        if (log.StatusCode < 400 || log.StatusCode > 599)
            throw new ArgumentException("An error status code is required.", nameof(log));
        error.Id = Guid.NewGuid();
        log.Id = Guid.NewGuid();
        log.ErrorId = error.Id;
        log.Timestamp = DateTime.UtcNow;
        log.Level = "Error";
        await errors.AddAsync(error, cancellationToken);
        await logs.AddAsync(log, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task LogRequestAsync(DatabaseLog log, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(log);
        if (log.StatusCode < 100 || log.StatusCode > 599)
            throw new ArgumentException("A valid HTTP status code is required.", nameof(log));
        log.Id = Guid.NewGuid();
        log.ErrorId = null;
        log.Timestamp = DateTime.UtcNow;
        log.Level = log.StatusCode >= 500 ? "Error" : log.StatusCode >= 400 ? "Warning" : "Information";
        await logs.AddAsync(log, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
