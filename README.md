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

### Verification OTP configuration

Mobile/email verification codes are generated with a cryptographically secure random generator and stored only as HMAC hashes. Configure a private verification hash key outside source control:

```text
Verification__HashKey=<at-least-32-byte-random-secret>
```

Development may use the application log to test delivery without a third-party provider. Production deliberately rejects delivery until a real SMS/email provider is configured. OTP values are never returned by the API and must never be logged in production. The flow enforces a 60-second resend cooldown, 10-minute code lifetime, and five incorrect attempts per challenge.

The API exposes:

- `GET /api/health` — application health
- `GET /api/health/database` — PostgreSQL connectivity status

The database health endpoint returns `not-configured` when no connection string is supplied and does not expose connection details.


## Safety, privacy and operations

The authenticated workspace includes profile visibility controls, a personal safety status view, report tracking, and server-enforced blocking/moderation. Contact details and verification documents are not exposed through profile discovery.

Administrators have growth/safety analytics and in-process API runtime metrics. API requests receive an `X-Correlation-ID`; server errors are logged with the correlation ID for operational tracing. Runtime metrics are intentionally process-local and are not a substitute for persistent production monitoring.

Report intake uses automated risk signals for scam/impersonation/payment/credential indicators and repeated reports. High-risk cases enter the reviewer queue as `Reviewing`; automated scoring does not suspend or ban accounts.

Notification badges use a server-side unread summary and refresh while the page is visible. 
