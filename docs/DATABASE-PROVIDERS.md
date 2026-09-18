# Configurable database providers

MilanSetu keeps one EF Core domain model and one set of table names/relationships while allowing the database provider to be selected by configuration.

## Supported providers

- `PostgreSQL` / `Postgres`
- `MySQL`

Set `Database:Provider=PostgreSQL` for Npgsql or `Database:Provider=MySQL` for Pomelo MySQL.

The connection string remains `ConnectionStrings:DefaultConnection`.

## Example configuration

PostgreSQL:
```
Database__Provider=PostgreSQL
ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=milansetu;Username=milansetu;Password=...
```

MySQL 8:
```
Database__Provider=MySQL
ConnectionStrings__DefaultConnection=Server=localhost;Port=3306;Database=milansetu;User=milansetu;Password=...
```

The API targets MySQL 8.x through Pomelo's EF Core provider.

## Docker

PostgreSQL:
```
DATABASE_PROVIDER=PostgreSQL \
DATABASE_CONNECTION_STRING='Host=postgres;Port=5432;Database=milansetu;Username=milansetu;Password=change-me-local-only' \
docker compose --profile postgres up --build
```

MySQL:
```
DATABASE_PROVIDER=MySQL \
DATABASE_CONNECTION_STRING='Server=mysql;Port=3306;Database=milansetu;User=milansetu;Password=change-me-local-only' \
docker compose --profile mysql up --build
```

The compose file keeps PostgreSQL and MySQL as separate profiles so the developer can choose one without starting both database servers.

## Schema compatibility

The same `MilanSetuDbContext` and domain entities are used for both providers. Existing logical table names, keys, indexes, foreign-key relationships and entity properties are preserved.

Provider-specific migrations must still be generated and reviewed separately because EF Core migrations contain provider-specific SQL/type mappings. Do not copy a PostgreSQL migration and assume it is valid MySQL SQL.

For a fresh environment, generate migrations using the selected provider and review the resulting SQL/schema before applying it. Existing production data must be backed up before any schema change.

## Repository pattern

Application services can depend on `IUnitOfWork`/`IRepository<TEntity>` rather than coupling simple persistence operations to EF Core. Complex EF queries may remain context-based where includes/projections are clearer. This keeps the repository abstraction useful instead of forcing every query through an overly generic API.
