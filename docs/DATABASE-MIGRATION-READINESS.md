# Database migration readiness

MilanSetu uses one provider-neutral MilanSetuDbContext and supports PostgreSQL and MySQL through Database:Provider.

## Design-time migrations

The design-time factory lets EF Core tooling create the same model for either provider without requiring a running database.

Examples:

    dotnet ef migrations add InitialPostgreSql --context MilanSetuDbContext --provider=PostgreSql --output-dir Data/Migrations/PostgreSql
    dotnet ef migrations add InitialMySql --context MilanSetuDbContext --provider=MySQL --output-dir Data/Migrations/MySql

These are scaffolding commands. Provider-specific migration sets should be generated and reviewed before being committed; application startup does not generate migrations automatically.

## Runtime database updates

Production schema changes must use reviewed EF Core migrations or an equivalent reviewed SQL deployment process. Do not use Database.EnsureCreated() for production because it bypasses migration history.

Both providers use the same logical table names, keys, relationships, indexes, and entity properties from MilanSetuDbContext, while provider-specific migrations may contain different SQL/type mappings.

## Configuration

Use:
- Database:Provider
- ConnectionStrings:DefaultConnection

Supported provider values:
- PostgreSql, Postgres, Npgsql
- MySQL, MariaDB

Do not commit production credentials.

## Workflow

1. Update the entity/model.
2. Scaffold a PostgreSQL migration.
3. Scaffold a MySQL migration.
4. Review both migration sets and generated SQL.
5. Apply the corresponding migration set to each environment.
6. Verify /api/health/database and application smoke tests.
7. Commit reviewed migrations with the model change.
