using Enerweb.Domain.Models;
using Enerweb.Interfaces.Services;

namespace Enerweb.Controllers;

public sealed class ReportsController : CrudController<ReportRequest>
{
    public ReportsController(IReportService service) : base(service) { }
    protected override Guid GetId(ReportRequest entity) => entity.Id;
    protected override void SetId(ReportRequest entity, Guid id) => entity.Id = id;
}
