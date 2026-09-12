# Database schema updates

The application uses EF Core with PostgreSQL. Messaging, safety, profile verification, and OTP verification introduce their tables and indexes through `MilanSetuDbContext`.

Before applying these features to a deployed database, generate and review an EF Core migration from the `backend/MilanSetu.Api` project:

```bash
dotnet ef migrations add AddVerificationOtpFoundation
dotnet ef database update
```

Migrations are intentionally generated in the deployment environment rather than committed from an environment that may not have the project's exact database provider/runtime tooling.

## OTP security

- OTPs are cryptographically generated and stored only as SHA-256 hashes.
- Codes expire after 10 minutes and are limited to 5 failed attempts.
- Resend requests are throttled by a 60-second cooldown.
- OTP values and destinations are never written to application logs or returned by API responses.
- SMS/email delivery is behind `IVerificationCodeSender`; the default implementation deliberately fails closed until a real provider is configured.
