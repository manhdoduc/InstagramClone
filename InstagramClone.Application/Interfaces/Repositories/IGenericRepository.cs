using System.Linq.Expressions;

namespace InstagramClone.Application.Interfaces.Repositories;

public interface IGenericRepository<T> where T : class
{
    IQueryable<T> Query(Expression<Func<T, bool>>? predicate = null);
    /// <summary>
    /// Returns a no-tracking queryable for read-only scenarios (lists, feeds, search).
    /// Use <see cref="Query"/> when the entity will be modified and saved.
    /// </summary>
    IQueryable<T> QueryNoTracking(Expression<Func<T, bool>>? predicate = null);
    Task<T?> GetByIdAsync(params object[] keyValues);
    void Add(T entity);
    void AddRange(IEnumerable<T> entities);
    void Update(T entity);
    void Remove(T entity);
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);
}
