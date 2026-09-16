# MilanSetu production deployment

This document is the release checklist for deploying MilanSetu. The application is intentionally fail-closed when required production configuration is missing.

## 1. Required production configuration

Set these values through the hosting platform's environment/secret configuration; do not commit production secrets.

- `ASPNETCORE_ENVIRONMENT=Production`
- `ConnectionStrings__DefaultConnection` — PostgreSQL connection string
- `Auth__Jwt__Key` — random secret with at least 32 UTF-8 bytes
- `Auth__Jwt__Issuer`
- `Auth__Jwt__Audience`
- `Cors__AllowedOrigins__0`, `Cors__AllowedOrigins__1`, ... — exact HTTPS frontend origins
- `Verification__HashKey` — strong secret used by verification/OTP services where required by the current implementation
- `VerificationDocuments__RootPath` — private writable storage path when filesystem storage is used

Production startup fails if the database connection, JWT key/issuer/audience, or CORS origins are missing.

## 2. Private verification documents

Verification documents must never be exposed as public/static files. The current filesystem implementation stores them under a private application path and validates extension, content type and a 10 MB maximum size.

The current scanner implementation is deliberately fail-closed (`ScannerNotConfigured`). Before accepting real production verification documents, configure a real malware/content scanner behind `IVerificationDocumentScanner` and verify that uploads remain quarantined until a safe verdict is returned.

For container/cloud deployments, prefer a durable private volume or private object storage rather than the container's ephemeral filesystem.

## 3. Database

Apply EF Core migrations as a controlled deployment step before enabling application traffic. Take a verified backup before schema changes. Do not make automatic destructive schema changes part of application startup.

The `/api/health` endpoint is a lightweight liveness check. `/api/health/database` checks database connectivity and returns HTTP 503 when the configured database cannot be reached.

## 4. HTTPS and proxy

Terminate TLS at the platform/load balancer and forward the original scheme/IP correctly. The API enables forwarded-header processing, HTTPS redirection and HSTS in Production.

The API also adds baseline security headers (`X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, and `Permissions-Policy`).

## 5. CI/CD gate

Every push and pull request to `main` must pass frontend build and backend restore/build/test/publish. The backend CI job also produces a release artifact for inspection.

A deployment workflow should deploy only a successful, reviewed commit. Keep deployment credentials in the platform's secret store. For Azure App Service, Microsoft recommends GitHub Actions with secret/identity-backed authentication rather than putting credentials in source control.

## 6. Go-live smoke test

After deployment:

1. `GET /api/health` returns 200.
2. `GET /api/health/database` returns 200.
3. Login and token issuance work.
4. Mobile/email OTP delivery uses a real configured provider and verification limits behave correctly.
5. Reviewer authorization is enforced from the persisted server-side role.
6. Verification document upload is private, quarantined and scanner-gated.
7. Unauthorized document access is rejected.
8. Messaging, discovery and profile flows work from the production frontend.
9. Rate limiting returns 429 under abuse thresholds.
10. Logs contain no passwords, OTP values, JWT secrets or private document contents.
11. Database backup/restore has been tested.
12. Rollback to the previous application version is documented and tested.

## 7. Release status

This branch is a deployment-hardening release candidate. Code-level readiness does not mean an actual production environment is configured. A real production release still requires infrastructure, DNS/TLS, database, secrets, OTP provider, private document storage/scanning, backups, monitoring and operational ownership to be configured and smoke-tested.
