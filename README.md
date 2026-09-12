# MilanSetu

A free, privacy-first matrimonial platform focused on meaningful compatibility, safety, and user control.

## Web-first development

Android is intentionally deferred. The first product is the responsive web application.

### Initial stack

- Frontend: React + TypeScript + Vite
- Backend: ASP.NET Core Web API + C#
- Database: PostgreSQL + Entity Framework Core
- Real-time messaging: SignalR (later milestone)

## Product principles

- Free for users: no subscriptions, premium profiles, paid messaging, or boosts.
- Privacy by design: contact details and sensitive verification documents are never public.
- User-controlled preferences: location, religion, community, caste, language, lifestyle, and marriage expectations are explicit preferences rather than hidden assumptions.
- Safety first: reporting, blocking, abuse detection, scam detection, and moderation are first-class capabilities.

## Development workflow

Feature branches are used for implementation. Changes are built/tested and merged through pull requests; `main` remains the stable branch.

## Local backend database configuration

The API uses PostgreSQL when a `ConnectionStrings:DefaultConnection` value is configured. Keep credentials outside source control.

For local development, set the connection string through user secrets or an environment-specific configuration source, for example:

```text
Host=localhost;Port=5432;Database=milansetu;Username=milansetu;Password=<local-password>
```

### Verification OTP

Mobile/email verification uses cryptographically secure six-digit codes. Codes are stored only as HMAC hashes using the configured JWT signing secret, expire after 10 minutes, and are invalidated after five failed attempts. Resends are throttled for 60 seconds.

The current delivery adapter intentionally has no third-party SMS/email dependency. It returns a safe temporary-unavailable response until a real delivery provider is configured. OTP values are never returned by the API or logged.

The API exposes:

- `GET /api/health` — application health
- `GET /api/health/database` — PostgreSQL connectivity status

The database health endpoint returns `not-configured` when no connection string is supplied and does not expose connection details.
