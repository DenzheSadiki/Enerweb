using Enerweb.Domain.Models;
using Enerweb.Interfaces.Repositories;

namespace Enerweb.Data.Repositories;

public sealed class DatabaseLogRepository : BaseRepository<DatabaseLog>, IDatabaseLogRepository
{
    public DatabaseLogRepository(EnerwebDbContext context) : base(context) { }
}
