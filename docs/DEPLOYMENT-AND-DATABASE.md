# Deployment and database operations

## API container

The API has a provider-neutral Docker image. Build it from the repository root with:
docker build -t milansetu-api ./backend/MilanSetu.Api

The image listens on port 8080. Put TLS at the hosting/reverse-proxy layer and forward the original HTTPS scheme to ASP.NET Core.

## Configurable database providers

The API supports PostgreSQL and MySQL through the same EF Core domain model.

Set:
- `Database:Provider=PostgreSQL` (default), or
- `Database:Provider=MySQL`

Use `ConnectionStrings:DefaultConnection` for either provider. The domain entities, logical table names, keys, indexes and relationships remain shared; provider-specific EF mappings are selected only at startup.

See `docs/DATABASE-PROVIDERS.md` for MySQL/PostgreSQL environment and Docker examples.

## Local PostgreSQL + API

From the repository root:
docker compose --profile postgres up --build

This starts PostgreSQL on port 5432 and the API on port 7001. The compose file is for local development only; replace all example secrets before using any shared environment.

## Local MySQL + API

From the repository root:
DATABASE_PROVIDER=MySQL DATABASE_CONNECTION_STRING='Server=mysql;Port=3306;Database=milansetu;User=milansetu;Password=change-me-local-only' docker compose --profile mysql up --build

This starts MySQL 8 on port 3306 and the API on port 7001.

## EF Core schema

The repository currently has no committed EF migration history. Do not use EnsureCreated for production because it bypasses migrations.

Migrations are provider-specific. Generate and review migrations against the selected provider rather than copying PostgreSQL migration SQL to MySQL.

For a new PostgreSQL database, generate a reviewed migration from the API project using the exact .NET/EF Core versions used by CI:
cd backend/MilanSetu.Api
dotnet tool install --global dotnet-ef --version 8.0.8
dotnet ef migrations add InitialSchema
dotnet ef database update

For MySQL, set `Database:Provider=MySQL` and a MySQL `ConnectionStrings:DefaultConnection` before generating the migration. Keep separate provider-specific migration histories/outputs and review the generated SQL before applying it.

Commit reviewed migrations only after confirming that the schema preserves the existing logical table structure. Future schema changes must use a new migration and `dotnet ef database update`.

For production, run migrations as a release step before switching traffic to the new API version. Keep the database connection string outside source control.

## Production inputs

The application still requires external infrastructure and secrets:
- PostgreSQL or MySQL connection string
- `Database:Provider`
- JWT key, issuer and audience
- verification HMAC key
- exact CORS frontend origin
- HTTPS API hostname exposed through the chosen provider
- real SMS/email provider credentials for production OTP delivery

These values are intentionally not committed to the repository.

## Automated database validation

CI validates that the API can build and its test suite executes on every pull request. Production must use reviewed EF Core migrations; `EnsureCreated` is intentionally not used by the API startup path.
