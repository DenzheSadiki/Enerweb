using Enerweb.Interfaces;
using Enerweb.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Enerweb.Services;

public abstract class BaseService<TEntity> : IBaseService<TEntity> where TEntity : class
{
    protected readonly IBaseRepository<TEntity> Repository;
    protected readonly IUnitOfWork UnitOfWork;

    protected BaseService(IBaseRepository<TEntity> repository, IUnitOfWork unitOfWork)
        => (Repository, UnitOfWork) = (repository, unitOfWork);

    protected abstract Guid GetId(TEntity entity);
    protected abstract Task ValidateAsync(TEntity entity);
    protected abstract void ApplyChanges(TEntity source, TEntity target);
    protected virtual Task ValidateDeleteAsync(TEntity entity) => Task.CompletedTask;

    public Task<TEntity?> GetByIdAsync(Guid id)
    {
        RequireId(id);
        return Repository.GetByIdAsync(id);
    }

    public Task<List<TEntity>> GetAllAsync() => Repository.Query().ToListAsync();

    public virtual async Task CreateAsync(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        await ValidateAsync(entity);
        if (GetId(entity) != Guid.Empty && await Repository.GetByIdAsync(GetId(entity)) != null)
            throw new InvalidOperationException("An entity with this ID already exists.");
        await Repository.AddAsync(entity);
        await UnitOfWork.SaveChangesAsync();
    }

    public async Task UpdateAsync(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        RequireId(GetId(entity));
        var existing = await Repository.GetByIdAsync(GetId(entity))
            ?? throw new KeyNotFoundException("The entity does not exist.");
        await ValidateAsync(entity);
        ApplyChanges(entity, existing);
        await UnitOfWork.SaveChangesAsync();
    }

    public async Task DeleteAsync(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        RequireId(GetId(entity));
        var existing = await Repository.GetByIdAsync(GetId(entity))
            ?? throw new KeyNotFoundException("The entity does not exist.");
        await ValidateDeleteAsync(existing);
        Repository.Delete(existing);
        await UnitOfWork.SaveChangesAsync();
    }

    protected static void RequireId(Guid id)
    {
        if (id == Guid.Empty) throw new ArgumentException("An ID is required.", nameof(id));
    }

    protected static void RequireText(string? value, string name, int? maxLength = null)
    {
        if (string.IsNullOrWhiteSpace(value) || (maxLength.HasValue && value.Length > maxLength))
            throw new ArgumentException($"{name} is required and must meet its length limit.", name);
    }
}
