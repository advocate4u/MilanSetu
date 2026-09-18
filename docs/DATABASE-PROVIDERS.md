# Configurable database providers

MilanSetu keeps one EF Core domain model and one set of table names/relationships while allowing the database provider to be selected by configuration.

## Supported providers

- `PostgreSQL` / `Postgres`
- `MySQL`

Set `Database:Provider=PostgreSQL` for Npgsql or `Database:Provider=MySQL` for Pomelo MySQL. The connection string remains `ConnectionStrings:DefaultConnection`.

## Docker

Existing PostgreSQL development flow:
```bash
docker compose up --build
```

MySQL development flow:
```bash
docker compose -f docker-compose.mysql.yml up --build
```

The MySQL compose file exposes the API on port 7002 so the PostgreSQL and MySQL stacks can be run independently.

## Schema compatibility

The same `MilanSetuDbContext` and domain entities are used for both providers. Existing logical table names, keys, indexes, foreign-key relationships and entity properties are preserved.

Provider-specific migrations must still be generated and reviewed separately because EF Core migrations contain provider-specific SQL/type mappings. Do not copy a PostgreSQL migration and assume it is valid MySQL SQL.

## Repository pattern

Application services can depend on `IUnitOfWork`/`IRepository<TEntity>` rather than coupling simple persistence operations to EF Core. Complex EF queries may remain context-based where includes/projections are clearer.
