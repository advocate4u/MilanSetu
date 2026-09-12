# Database schema updates

The application uses EF Core with PostgreSQL. Messaging introduces the `conversations` and `messages` tables and verification introduces the `verification_challenges` table plus its indexes/relationships in `MilanSetuDbContext`.

Before applying these features to a deployed database, generate and review an EF Core migration from the `backend/MilanSetu.Api` project:

```bash
dotnet ef migrations add AddVerificationOtp
dotnet ef database update
```

Migrations are intentionally generated in the deployment environment rather than committed from an environment that may not have the project's exact database provider/runtime tooling.
