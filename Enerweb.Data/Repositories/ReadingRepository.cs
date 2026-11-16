using Enerweb.Domain.Models;
using Enerweb.Interfaces.Repositories;

namespace Enerweb.Data.Repositories;

public sealed class ReadingRepository : BaseRepository<Reading>, IReadingRepository
{
    public ReadingRepository(EnerwebDbContext context) : base(context) { }
}
