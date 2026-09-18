# Deployment and database operations

## API container

The API has a provider-neutral Docker image. Build it from the repository root with:
docker build -t milansetu-api ./backend/MilanSetu.Api

The image listens on port 8080. Put TLS at the hosting/reverse-proxy layer and forward the original HTTPS scheme to ASP.NET Core.

## Local PostgreSQL + API

From the repository root:
docker compose up --build

This starts PostgreSQL on port 5432 and the API on port 7001. The compose file is for local development only; replace all example secrets before using any shared environment.

## EF Core schema

The repository currently has no committed EF migration history. Do not use EnsureCreated for production because it bypasses migrations.

For a new database, generate a reviewed migration from the API project using the exact .NET/EF Core versions used by CI:
cd backend/MilanSetu.Api
dotnet tool install --global dotnet-ef --version 8.0.8
dotnet ef migrations add InitialSchema
dotnet ef database update

Commit the generated Migrations/ directory after review. Future schema changes must use a new migration and dotnet ef database update.

For production, run migrations as a release step before switching traffic to the new API version. Keep the database connection string outside source control.

## Production inputs

The application still requires external infrastructure and secrets:
- PostgreSQL connection string
- JWT key, issuer and audience
- verification HMAC key
- exact CORS frontend origin
- HTTPS API hostname exposed through the chosen provider
- real SMS/email provider credentials for production OTP delivery

These values are intentionally not committed to the repository.
