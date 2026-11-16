using Enerweb.Domain.Models;
using Enerweb.Interfaces.Repositories;

namespace Enerweb.Data.Repositories;

public sealed class ErrorRepository : BaseRepository<Error>, IErrorRepository
{
    public ErrorRepository(EnerwebDbContext context) : base(context) { }
}
