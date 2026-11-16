using Enerweb.Domain.Models;
using Enerweb.Interfaces.Repositories;

namespace Enerweb.Data.Repositories;

public sealed class MeterRepository : BaseRepository<Meters>, IMeterRepository
{
    public MeterRepository(EnerwebDbContext context) : base(context) { }
}
