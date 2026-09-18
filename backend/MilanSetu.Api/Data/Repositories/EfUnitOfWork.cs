namespace MilanSetu.Api.Data.Repositories;

public sealed class EfUnitOfWork(MilanSetuDbContext db) : IUnitOfWork
{
    private readonly Dictionary<Type, object> repositories = new();

    public IRepository<TEntity> Repository<TEntity>() where TEntity : class
    {
        var type = typeof(TEntity);
        if (!repositories.TryGetValue(type, out var repository))
        {
            repository = new EfRepository<TEntity>(db);
            repositories[type] = repository;
        }

        return (IRepository<TEntity>)repository;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken);
}
