# Email OTP configuration

MilanSetu email verification uses SMTP for OTP delivery. Mobile OTP remains intentionally disabled until a mobile delivery provider is selected.

Configure these environment variables (or equivalent ASP.NET Core configuration keys) for the API:

```text
Email__Smtp__Host=smtp.example.com
Email__Smtp__Port=587
Email__Smtp__Username=mailer@example.com
Email__Smtp__Password=<secret>
Email__Smtp__FromAddress=mailer@example.com
Email__Smtp__FromName=MilanSetu
Email__Smtp__EnableSsl=true
Verification__HashKey=<random-secret-at-least-32-bytes>
```

Do not commit SMTP credentials or the verification hash key to the repository. GitHub recommends storing sensitive workflow credentials as encrypted secrets rather than plaintext configuration. urlGitHub Actions secrets documentationhttps://docs.github.com/en/actions/reference/security/secrets

The email OTP flow is:

1. Authenticated user requests `POST /api/verification/Email/request`.
2. The API generates a cryptographically random 6-digit code.
3. Only an HMAC hash is persisted; the plaintext code is sent by SMTP.
4. The code expires after 10 minutes, with a 60-second resend cooldown and a maximum of 5 failed attempts.
5. `POST /api/verification/Email/verify` marks the email verification as complete.

In development, if SMTP is not configured, the code is logged to the server output for local testing. In production, email delivery fails closed until SMTP is configured.
