# MilanSetu Production Deployment

## Deployment model

MilanSetu is split into:
- ASP.NET Core 8 API: `backend/MilanSetu.Api`
- React/Vite web client: `frontend`
- PostgreSQL or MySQL/MariaDB database
- persistent storage for profile photos and verification documents

The application is free for users. Advertising remains controlled by SuperAdmin.

## 1. Required production configuration

Set these values through the hosting provider's secret/environment configuration; do not commit them:

- `ConnectionStrings__DefaultConnection`
- `Database__Provider`: `PostgreSQL` or `MySQL`
- `Auth__Jwt__Key`: at least 32 UTF-8 bytes
- `Auth__Jwt__Issuer`
- `Auth__Jwt__Audience`
- `Verification__HashKey`: at least 32 UTF-8 bytes
- `Cors__AllowedOrigins__0`: the exact HTTPS web origin
- Google/Facebook authentication settings when those providers are enabled
- `SuperAdmin__Email` only during initial SuperAdmin provisioning

Remove `SuperAdmin__Email` after the first successful provisioning/restart when practical.

## 2. Database deployment

CI currently generates provider-specific EF Core migrations and validates them against clean PostgreSQL and MySQL databases.

For a **new empty database**, use the generated `001_InitialCreate.sql` artifact:
- PostgreSQL: `artifacts/initial-migrations/postgresql/001_InitialCreate.sql`
- MySQL: `artifacts/initial-migrations/mysql/001_InitialCreate.sql`

Apply it only after taking the normal database backup/snapshot.

### Existing production database

Do not apply `001_InitialCreate.sql` blindly to an existing database. The repository currently has a baseline-generation workflow rather than a committed incremental migration history. Existing installations should first be reconciled against the current EF model/schema, then moved to versioned migrations.

Never use `EnsureCreated` for production schema upgrades.

## 3. Backend

Build/publish:
- Windows: `powershell -File scripts/deployment/publish-api.ps1`
- Linux: `bash scripts/deployment/publish-api.sh`

Host the published ASP.NET Core application behind HTTPS. Configure the reverse proxy/IIS to forward HTTPS correctly.

Health endpoints:
- `/api/health`
- `/api/health/database`

Verify both after deployment.

## 4. Frontend

Build:
- Windows: `powershell -File scripts/deployment/publish-frontend.ps1`
- Linux: `bash scripts/deployment/publish-frontend.sh`

Serve the generated static files through the web server/CDN. Configure the frontend API base URL for the production API origin when required.

## 5. Persistent storage

Back up and persist:
- `App_Data/profile-photos`
- `App_Data/verification-documents`

These directories must not live only inside an ephemeral container/filesystem. Keep access private and serve files through the application's authorization-controlled endpoints.

## 6. Release order

1. Backup database and persistent files.
2. Deploy database changes.
3. Deploy API.
4. Verify database/API health.
5. Deploy frontend.
6. Verify login, registration, profile, discovery, messaging, contact-sharing, photos and SuperAdmin controls.
7. Monitor application and database logs.

If a migration fails, stop the release and restore/reconcile before deploying the application version that depends on it.

## 7. Security checklist

- HTTPS only.
- Strong unique JWT and verification keys.
- Exact production CORS origins.
- Database not publicly exposed unless required by the hosting architecture.
- Secrets stored in host secret storage, not Git.
- Regular database and persistent-file backups.
- Least-privilege database account.
- Review reverse-proxy forwarded-header configuration.
- Review CSP whenever a new external service is enabled.
- Do not expose profile photos or verification documents by direct filesystem path.

## 8. Hosting

The repository does not require a specific cloud provider. Windows/IIS, Linux/reverse-proxy, or container hosting can be used as long as .NET 8, the selected database, HTTPS and persistent storage are supported.

For GoDaddy Windows hosting, confirm that the selected plan supports ASP.NET Core/.NET 8 hosting and persistent writable storage before deployment.
