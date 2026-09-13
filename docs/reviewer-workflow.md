# Reviewer / Admin verification workflow

MilanSetu keeps reviewer authorization server-side. Normal registration never accepts a role, and the browser cannot promote an account.

## Roles

`User` is the default application role. `Reviewer` can review Identity, Education and Employment requests. `Admin` has the same reviewer capabilities in this milestone; admin-specific user/role management remains a later hardening task.

Roles are stored in `user_role_assignments` and checked against the database on every reviewer request rather than trusting a client-provided value or a stale JWT claim.

## First reviewer bootstrap

Because role elevation is security-sensitive, do not add a public bootstrap endpoint and do not hard-code credentials. After creating the operator account through the normal registration flow, grant the role through a protected database/operations process.

Example PostgreSQL statement (replace the email with the already-created operator account):

```sql
INSERT INTO user_role_assignments (user_id, role, created_at)
SELECT id, 'Reviewer', NOW()
FROM users
WHERE email = 'operator@example.invalid'
ON CONFLICT (user_id) DO UPDATE SET role = EXCLUDED.role, updated_at = NOW();
```

The deployment operator should run this only through the secured database administration path. Never expose this SQL or a database credential to end users.

## Reviewer API

- `GET /api/reviewer/verifications?type=&status=&page=&pageSize=` — pending queue by default.
- `GET /api/reviewer/verifications/{id}` — minimal request details; no document bytes or email/phone fields.
- `POST /api/reviewer/verifications/{id}/claim` — claims a pending item for 30 minutes.
- `POST /api/reviewer/verifications/{id}/approve` with `{ "notes": "..." }`.
- `POST /api/reviewer/verifications/{id}/reject` with `{ "notes": "reason" }`; a rejection reason is mandatory.

Mobile and email OTP verification are intentionally excluded from reviewer decisions because they are completed by the OTP challenge flow.

## Audit

Queue/detail access and claim/approve/reject actions are written to `audit_logs`. Notes are stored as structured JSON metadata. Do not put passwords, OTPs, document contents, or unnecessary personal data into audit metadata.

## Database update

This repository intentionally does not commit EF migrations. Generate the migration in the deployment/development environment after pulling this change, for example:

```bash
dotnet ef migrations add AddReviewerVerificationWorkflow --project backend/MilanSetu.Api --startup-project backend/MilanSetu.Api
dotnet ef database update --project backend/MilanSetu.Api --startup-project backend/MilanSetu.Api
```

Review the generated migration before applying it to production.
