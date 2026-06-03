using System.Linq.Expressions;
using InstagramClone.Application.Interfaces.Repositories;
using InstagramClone.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InstagramClone.Infrastructure.Repositories;

public class GenericRepository<T>(AppDbContext context) : IGenericRepository<T> where T : class
{
    private readonly DbSet<T> _dbSet = context.Set<T>();

    public IQueryable<T> Query(Expression<Func<T, bool>>? predicate = null)
    {
        var query = _dbSet.AsQueryable();
        if (predicate != null)
        {
            query = query.Where(predicate);
        }
        return query;
    }

    public IQueryable<T> QueryNoTracking(Expression<Func<T, bool>>? predicate = null)
    {
        var query = _dbSet.AsNoTracking().AsQueryable();
        if (predicate != null)
        {
            query = query.Where(predicate);
        }
        return query;
    }

    public async Task<T?> GetByIdAsync(params object[] keyValues)
    {
        return await _dbSet.FindAsync(keyValues);
    }// dùng params cho phép query khóa chính đơn hoặc khóa kết hợp

    public void Add(T entity)
    {
        _dbSet.Add(entity);
    }

    public void AddRange(IEnumerable<T> entities)
    {
        _dbSet.AddRange(entities);
    }

    public void Update(T entity)
    {
        _dbSet.Update(entity);
    }

    public void Remove(T entity)
    {
        _dbSet.Remove(entity);
    }

    public async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.AnyAsync(predicate);
    }
}
