using System.Threading;
using System.Threading.Tasks;
using Enerweb.Interfaces;

namespace Enerweb.Data;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly EnerwebDbContext context;
    public UnitOfWork(EnerwebDbContext context) => this.context = context;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => context.SaveChangesAsync(cancellationToken);
}
