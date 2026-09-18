# Database initialization

MilanSetu uses EF Core with PostgreSQL.

For a **new empty database**, the API can create the current EF model automatically by setting:

`Database__EnsureCreated=true`

This is intended for local development, smoke environments, and first-time bootstrap only. It uses EF Core `EnsureCreated` and therefore must not be mixed with an existing EF migration history.

Example:

```bash
Database__EnsureCreated=true dotnet run --project backend/MilanSetu.Api
```

For an existing production database, use reviewed EF Core migrations rather than `EnsureCreated`. The repository currently has no committed migration history, so production schema creation/migration must be generated and reviewed against the exact deployed model before applying it.

The database health endpoint remains:

`GET /api/health/database`

A successful response confirms that the configured PostgreSQL database is reachable.
