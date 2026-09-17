# MilanSetu production deployment

GitHub Pages hosts only the React/Vite frontend. The ASP.NET Core API and PostgreSQL database must be hosted separately over HTTPS.

## Required production topology

```text
Browser
  -> https://advocate4u.github.io/MilanSetu/
  -> HTTPS MilanSetu API
  -> PostgreSQL
```

Do not point the production frontend at `localhost`. A browser opened from GitHub Pages resolves `localhost` to the user's own device.

## Frontend configuration

Set the GitHub repository Actions variable `VITE_API_BASE_URL` to the public HTTPS base URL of the deployed API, for example:

```text
https://api.example.com
```

The Pages workflow validates that this variable exists and uses HTTPS before building the site.

## API production configuration

The ASP.NET Core API requires these settings in production:

```text
ConnectionStrings__DefaultConnection=<PostgreSQL connection string>
Auth__Jwt__Key=<random secret, at least 32 UTF-8 bytes>
Auth__Jwt__Issuer=MilanSetu
Auth__Jwt__Audience=MilanSetu.Web
Cors__AllowedOrigins__0=https://advocate4u.github.io
Verification__HashKey=<random secret, at least 32 bytes>
```

Keep secrets in the hosting provider's secret/environment configuration. Never commit them to Git.

The API exposes:

- `GET /api/health` for application reachability.
- `GET /api/health/database` for PostgreSQL connectivity.
- `POST /api/auth/register` for registration.
- `POST /api/auth/login` for login.

## Database

The application uses PostgreSQL through Entity Framework Core. The production API requires a configured `ConnectionStrings:DefaultConnection` value. Database schema migrations must be generated/reviewed and applied in the deployment environment before registration is tested.

For a newly provisioned database, verify `/api/health/database` first and then test registration. A database connectivity failure is distinct from a browser `Failed to fetch` error: the latter normally means the browser could not reach the API endpoint at all.

## CORS and credentials

The frontend sends credentials for authentication requests. The API therefore must explicitly allow the GitHub Pages origin and credentials. Do not use `AllowAnyOrigin` with credentials.

## Deployment order

1. Provision PostgreSQL.
2. Deploy `backend/MilanSetu.Api` as an HTTPS ASP.NET Core service.
3. Configure the production API environment variables above.
4. Apply the EF Core schema/migrations.
5. Confirm `/api/health` returns `200`.
6. Confirm `/api/health/database` reports PostgreSQL `ok`.
7. Set the GitHub Actions repository variable `VITE_API_BASE_URL` to the API's public HTTPS URL.
8. Deploy GitHub Pages.
9. Test account registration from the Pages site.

The repository contains the backend Dockerfile, so a Docker-capable ASP.NET hosting provider can build the API directly from `backend/MilanSetu.Api/Dockerfile`.
