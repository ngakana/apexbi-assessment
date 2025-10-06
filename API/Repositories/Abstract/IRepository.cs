namespace API.Repositories.Abstract;

public interface IRepository<T> where T : class
{
    IQueryable<T> GetAsQueryable();
    Task<List<T>> GetAllAsync(CancellationToken ct = default);
    Task<T?> GetByIdAsync(int id, CancellationToken ct = default);
    Task AddAsync(T entity, CancellationToken ct = default);  
    void Add(T entity);
    Task AddRangeAsync(ICollection<T> entities, CancellationToken ct = default);
}
