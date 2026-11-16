namespace Enerweb.Interfaces;

public interface IUnitOfWork
{
    // Commits all staged repository changes in one atomic SaveChanges transaction.
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
