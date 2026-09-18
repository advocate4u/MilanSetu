using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace MilanSetu.Api.Data.Repositories;

public sealed class EfRepository<TEntity>(MilanSetuDbContext db) : IRepository<TEntity> where TEntity : class
{
    private DbSet<TEntity> Set => db.Set<TEntity>();

    public IQueryable<TEntity> Query(bool asNoTracking = true) =>
        asNoTracking ? Set.AsNoTracking() : Set;

    public Task<TEntity?> GetByIdAsync(object id, CancellationToken cancellationToken = default) =>
        Set.FindAsync([id], cancellationToken).AsTask();

    public Task<TEntity?> SingleOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default) =>
        Set.SingleOrDefaultAsync(predicate, cancellationToken);

    public Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default) =>
        Set.AnyAsync(predicate, cancellationToken);

    public void Add(TEntity entity) => Set.Add(entity);

    public void Remove(TEntity entity) => Set.Remove(entity);
}
