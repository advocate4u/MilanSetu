# Repository pattern

MilanSetu uses Entity Framework Core as the ORM and exposes a small repository/unit-of-work abstraction for application services that do not need database-specific behavior.

- `IRepository<TEntity>` provides query/add/remove operations.
- `IUnitOfWork` owns repository instances for a request and commits changes once.
- `EfRepository<TEntity>` and `EfUnitOfWork` are infrastructure implementations.
- Complex read queries may continue to use `MilanSetuDbContext` directly when EF-specific projections/includes are materially clearer. The repository layer is not intended to hide useful EF capabilities behind a leaky generic API.
- Existing table names, keys, relationships and domain entities remain unchanged.

The repository layer is provider-neutral: PostgreSQL and MySQL are selected by configuration while application/domain code continues to target the same entities.
