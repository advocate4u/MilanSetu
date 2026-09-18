# MilanSetu

A free, privacy-first matrimonial platform focused on meaningful compatibility, safety, and user control.

## Web-first development

Android is intentionally deferred. The first product is the responsive web application.

### Initial stack

- Frontend: React + TypeScript + Vite
- Backend: ASP.NET Core Web API + C#
- Database: PostgreSQL + Entity Framework Core
- Real-time messaging: SignalR with authenticated user channels and reconnect fallback

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

Notification badges use a server-side unread summary, refresh while the page is visible, and react to live SignalR events. Messaging also keeps polling as a resilience fallback. 

## Minimal advertising

MilanSetu remains free for users. The web app includes an optional, unobtrusive advertising panel that is disabled by default. It supports either a simple sponsor message/link or Google AdSense without exposing user profile data to the ad component.

Configure the frontend through environment variables in `frontend/.env` (never commit secrets):

- `VITE_AD_MODE=none` — disabled (default)
- `VITE_AD_MODE=sponsor` — direct sponsor message/link
- `VITE_AD_MODE=adsense` — Google AdSense slot
- `VITE_AD_TEXT`, `VITE_AD_URL`, `VITE_AD_IMAGE_URL` — sponsor creative
- `VITE_ADSENSE_CLIENT`, `VITE_ADSENSE_SLOT` — AdSense configuration

Ads are deliberately limited to the authenticated workspace and do not use MilanSetu profile data for targeting.

## Super Admin advertising controls

Advertising is configured from the Super Admin workspace rather than frontend environment variables. The single minimal panel is disabled until a Super Admin enables it. Sponsor or Google AdSense settings are stored server-side, and the public ad endpoint returns only the fields needed to render the approved advertisement.

For initial deployment, an existing account can be promoted once by setting the backend environment variable `SuperAdmin__Email` to that account's exact email. On startup, MilanSetu assigns that existing account the `SuperAdmin` role; it does not create accounts. Remove the bootstrap setting after provisioning if it is no longer needed.
