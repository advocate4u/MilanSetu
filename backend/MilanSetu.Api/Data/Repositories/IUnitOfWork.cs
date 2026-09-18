namespace MilanSetu.Api.Data.Repositories;

public interface IUnitOfWork
{
    IRepository<TEntity> Repository<TEntity>() where TEntity : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
