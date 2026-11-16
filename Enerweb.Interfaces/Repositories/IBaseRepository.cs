namespace Enerweb.Interfaces.Repositories;

public interface IBaseRepository<TEntity> where TEntity : class
{
    // Deferred queries must be executed within the owning scope. No tracking by default.
    IQueryable<TEntity> Query();
    IQueryable<TEntity> QueryTracking();
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    void Update(TEntity entity);
    void Delete(TEntity entity);
}
