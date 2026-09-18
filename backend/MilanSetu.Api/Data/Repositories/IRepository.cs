using System.Linq.Expressions;

namespace MilanSetu.Api.Data.Repositories;

public interface IRepository<TEntity> where TEntity : class
{
    IQueryable<TEntity> Query(bool asNoTracking = true);
    Task<TEntity?> GetByIdAsync(object id, CancellationToken cancellationToken = default);
    Task<TEntity?> SingleOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
    Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
    void Add(TEntity entity);
    void Remove(TEntity entity);
}
