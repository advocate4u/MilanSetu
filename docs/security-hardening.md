# API security hardening

The API now applies several production-oriented safeguards:

- A global per-client-IP request limiter returns HTTP 429 when the limit is exceeded.
- Authentication endpoints use a separate per-IP fixed-window limiter.
- Verification endpoints use a separate per-user/per-IP fixed-window limiter.
- Rate-limit responses include a Retry-After header.
- Production startup requires a 32-byte minimum JWT signing key and verification HMAC key.
- Production startup requires explicit JWT issuer/audience and CORS origins.
- Requests with a declared body larger than 10 MiB are rejected with HTTP 413.
- Production errors return a generic response rather than exception details.
- HSTS is enabled in production.
- Security response headers include X-Content-Type-Options, X-Frame-Options, Referrer-Policy, Permissions-Policy, Cross-Origin-Resource-Policy, Cross-Origin-Opener-Policy, and a restrictive API-oriented CSP.
- Refresh tokens remain HttpOnly, Secure in production, SameSite=Strict, path-scoped to /api/auth, and rotated on refresh.
- Password input is bounded to 12–128 characters to prevent unnecessary password-hashing resource consumption.
- OTP codes are cryptographically random, HMAC-protected at rest, expire after 10 minutes, and are capped at five failed attempts.

## External production requirements

Real SMS delivery still requires a selected SMS provider and credentials. Real email delivery requires SMTP credentials. These must be supplied through deployment secrets/environment variables and must not be committed to source control.

The frontend also requires the production HTTPS API base URL through the existing VITE_API_BASE_URL repository variable.

## Operational note

The in-process rate limiter is appropriate for a single API instance. If the API is horizontally scaled, move rate-limit state to a shared gateway/service or otherwise ensure abuse controls are enforced consistently across instances.
