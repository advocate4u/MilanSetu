# Reviewer verification workflow

The reviewer workflow covers only `Identity`, `Education`, and `Employment` verification. Mobile and email verification remain OTP-controlled by the user.

## Server-side reviewer authorization

Reviewer access is **not** controlled by the browser. The API checks the authenticated user's persisted account email against server configuration on every reviewer request.

Configure these values in the deployment environment (do not commit real addresses or secrets):

```json
{
  "Authorization": {
    "ReviewerEmails": ["reviewer@example.org"],
    "AdminEmails": ["admin@example.org"]
  }
}
```

Environment-variable equivalents can be supplied using ASP.NET Core configuration, for example `Authorization__ReviewerEmails__0` and `Authorization__AdminEmails__0`.

There is intentionally no public API for assigning reviewer/admin access, so an ordinary account cannot self-promote.

## Review flow

1. A user requests Identity, Education, or Employment verification.
2. The request enters `Pending` status.
3. An authorized reviewer sees the request in `GET /api/reviewer/verifications`.
4. The reviewer opens the request and may claim it for a short review window.
5. The reviewer approves or rejects it.
6. Rejection requires a reason.
7. The request records reviewer, timestamp, notes and decision in `verification_requests`.
8. A durable `ReviewerAuditEvent` is written for claim/approve/reject actions.

The reviewer queue deliberately does not return verification-document contents. Document storage/scanning is the next security milestone.

## Database update

The project does not commit environment-specific EF migrations. Before deploying this feature, generate/apply a migration for the added reviewer fields and `ReviewerAuditEvent` table using the normal deployment process, for example:

```bash
dotnet ef migrations add AddReviewerVerificationWorkflow
dotnet ef database update
```

Review the generated migration before applying it to production.
