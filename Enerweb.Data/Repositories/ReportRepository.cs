using Enerweb.Domain.Models;
using Enerweb.Interfaces.Repositories;

namespace Enerweb.Data.Repositories;

public sealed class ReportRepository : BaseRepository<ReportRequest>, IReportRepository
{
    public ReportRepository(EnerwebDbContext context) : base(context) { }
}
