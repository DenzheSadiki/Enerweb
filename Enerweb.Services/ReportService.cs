using Enerweb.Domain.Models;
using Enerweb.Interfaces;
using Enerweb.Interfaces.Repositories;
using Enerweb.Interfaces.Services;

namespace Enerweb.Services;

public sealed class ReportService : BaseService<ReportRequest>, IReportService
{
    public ReportService(IReportRepository repository, IUnitOfWork unitOfWork)
        : base(repository, unitOfWork) { }

    protected override Guid GetId(ReportRequest entity) => entity.Id;

    protected override Task ValidateAsync(ReportRequest entity)
    {
        RequireText(entity.Name, nameof(entity.Name), 200);
        RequireText(entity.Entity, nameof(entity.Entity), 100);
        RequireText(entity.QueryDefinition, nameof(entity.QueryDefinition));
        RequireId(entity.RequestedBy);
        // Query definitions are stored as data; this service does not execute them.
        return Task.CompletedTask;
    }

    public override Task CreateAsync(ReportRequest entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        entity.CreatedAt = DateTime.UtcNow;
        entity.LastExecutedAt = null;
        return base.CreateAsync(entity);
    }

    protected override void ApplyChanges(ReportRequest source, ReportRequest target)
    {
        target.Name = source.Name;
        target.Description = source.Description;
        target.Entity = source.Entity;
        target.QueryDefinition = source.QueryDefinition;
        target.IsSaved = source.IsSaved;
        // Preserve ownership, creation time, and execution history on updates.
    }
}
