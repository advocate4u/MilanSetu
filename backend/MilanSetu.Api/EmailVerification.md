# Email and mobile OTP configuration

MilanSetu sends email verification codes through SMTP and mobile verification codes through Twilio. Both delivery channels fail closed: if the relevant provider is not configured or rejects a message, the API returns a service-unavailable response and does not mark the destination as verified.

## Email SMTP configuration

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

## Mobile SMS configuration

Configure the Twilio credentials when mobile OTP delivery is enabled:

```text
Sms__Twilio__AccountSid=<twilio-account-sid>
Sms__Twilio__AuthToken=<twilio-auth-token>
Sms__Twilio__FromNumber=+1xxxxxxxxxx
```

Do not commit SMTP, Twilio, or verification hash credentials to the repository. Store them in the hosting provider's secret manager or encrypted deployment secrets.

## Verification flow

1. An authenticated user requests `POST /api/verification/Email/request` or `POST /api/verification/Mobile/request`.
2. The API generates a cryptographically random 6-digit code.
3. Only an HMAC hash is persisted; the plaintext code is sent through the configured provider.
4. The code expires after 10 minutes, with a 60-second resend cooldown and a maximum of 5 failed attempts.
5. The user submits the code to `POST /api/verification/{type}/verify`.
6. The API marks the destination as verified only after successful code verification.

If SMTP or Twilio is missing or unavailable, the API returns HTTP 503. It does not silently accept a verification request, log the OTP, or mark the email/mobile number as verified. Development environments must use a test SMTP/SMS provider or a mock sender for end-to-end verification tests.
