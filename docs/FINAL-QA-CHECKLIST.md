# MilanSetu final QA checklist

## Automated checks
- Frontend TypeScript compilation and production build
- Backend Release build, tests and publish
- API and frontend container builds
- CodeQL analysis

## Functional smoke flow
1. Register a new account.
2. Complete email/mobile verification with configured production providers.
3. Complete profile and upload/reorder photos.
4. Apply discovery filters and inspect results.
5. Send interest and accept/decline an incoming interest.
6. Confirm connection creation and messaging authorization.
7. Send/delete messages and verify notification delivery.
8. Block/unblock and confirm messaging/discovery restrictions.
9. Submit a report and verify the user's report status.
10. Verify reviewer and admin workflows with server-side authorization.
11. Verify privacy/safety settings and notification read state.

## Production checks
- HTTPS API and database health endpoints return healthy status.
- CORS allows only the deployed frontend origin.
- Production JWT and verification keys satisfy configured minimums.
- Request size and rate limits return 413/429 with Retry-After as applicable.
- Realtime reconnects after transient disconnects and falls back to normal refresh behavior.
- Database schema is deployed through reviewed EF migrations.
- Backups, restore procedure, monitoring and alerting are verified outside CI.

Production provider credentials and live E2E execution are intentionally external to source control.
