# MilanSetu release readiness

## Implemented in source

- Authenticated registration, login, refresh and logout lifecycle
- Profile, photo, discovery, interests and connections
- Messaging with moderation and blocking controls
- Notifications and safety/reporting workflows
- Reviewer and administrator workspaces with server-side authorization
- Privacy and safety status APIs
- Moderation automation and audit logging
- Request metrics and correlation IDs
- API rate limiting and security headers
- Production fail-closed configuration for JWT, verification HMAC and CORS

## Required before public production launch

1. Provision PostgreSQL and set ConnectionStrings:DefaultConnection.
2. Set Auth:Jwt:Key, Auth:Jwt:Issuer and Auth:Jwt:Audience.
3. Set Verification:HashKey to a strong secret of at least 32 UTF-8 bytes.
4. Configure CORS with the exact production frontend origin.
5. Configure VITE_API_BASE_URL in GitHub Pages repository variables.
6. Configure real email SMTP credentials if email OTP is enabled.
7. Select and configure an SMS provider before enabling mobile OTP.
8. Apply/verify the database schema against the production database.
9. Deploy the API over HTTPS and verify /api/health and /api/health/database.
10. Run an end-to-end smoke test: register -> verify -> profile -> discovery -> interest -> connection -> message -> notification -> report.
11. Verify reviewer/admin authorization with separate non-admin and admin accounts.
12. Verify backup/restore and operational monitoring before accepting real users.

## Important deployment constraint

The repository can contain the application and deployment configuration, but it cannot create third-party cloud accounts, PostgreSQL instances, DNS records, SMTP accounts, or SMS-provider credentials. Those are deployment-time dependencies and must be supplied securely.

## Scaling note

The current API rate limiter keeps state in-process. For multiple API instances, enforce equivalent limits at a shared gateway or distributed store.

## Release gate

Do not advertise the application as fully production-live until the external deployment checklist above is completed and the end-to-end smoke test passes against the real API and database.
