using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Enerweb.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Enerweb.Data.Repositories;

public class BaseRepository<TEntity> : IBaseRepository<TEntity> where TEntity : class
{
    private readonly DbSet<TEntity> entities;

    public BaseRepository(EnerwebDbContext context) => entities = context.Set<TEntity>();

    public IQueryable<TEntity> Query() => entities.AsNoTracking();
    public IQueryable<TEntity> QueryTracking() => entities;

    public async Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await entities.FindAsync(new object[] { id }, cancellationToken);

    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        await entities.AddAsync(entity, cancellationToken);
    }

    public void Update(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        entities.Entry(entity).State = EntityState.Modified;
    }

    public void Delete(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        entities.Remove(entity);
    }
}
