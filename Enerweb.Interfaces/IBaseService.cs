namespace Enerweb.Interfaces
{
    public interface IBaseService<T> where T : class
    {
        Task<T?> GetByIdAsync(Guid id);

        Task<List<T>> GetAllAsync();

        Task CreateAsync(T entity);

        Task UpdateAsync(T entity);

        Task DeleteAsync(T entity);
    }
}
