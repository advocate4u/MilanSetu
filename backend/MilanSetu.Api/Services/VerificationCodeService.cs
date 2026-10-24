using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using MilanSetu.Api.Domain;

namespace MilanSetu.Api.Services;

public interface IVerificationCodeSender
{
    Task SendAsync(VerificationType type, string destination, string code, CancellationToken cancellationToken);
}

public sealed class VerificationCodeSender(
    IHostEnvironment environment,
    IConfiguration configuration,
    ILogger<VerificationCodeSender> logger,
    IHttpClientFactory httpClientFactory) : IVerificationCodeSender
{
    private static readonly Regex E164Pattern = new(@"^\+[1-9][0-9]{7,14}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public async Task SendAsync(VerificationType type, string destination, string code, CancellationToken cancellationToken)
    {
        if (type == VerificationType.Mobile)
        {
            await SendMobileAsync(destination, code, cancellationToken);
            return;
        }

        if (type != VerificationType.Email)
            throw new InvalidOperationException("Unsupported verification delivery type.");

        var host = configuration["Email:Smtp:Host"];
        var username = configuration["Email:Smtp:Username"];
        var password = configuration["Email:Smtp:Password"];
        var fromAddress = configuration["Email:Smtp:FromAddress"];

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(fromAddress))
        {
            if (environment.IsDevelopment())
            {
                logger.LogWarning(
                    "Email verification delivery is not configured in Development; OTP was not logged for destination {Destination}.",
                    MaskEmail(destination));
                return;
            }

            throw new InvalidOperationException("Email SMTP delivery is not configured for this environment.");
        }

        if (!MailAddress.TryCreate(destination, out var recipient))
            throw new InvalidOperationException("The configured email address is invalid.");
        if (!MailAddress.TryCreate(fromAddress, out var senderAddress))
            throw new InvalidOperationException("Email:Smtp:FromAddress is invalid.");

        var port = configuration.GetValue("Email:Smtp:Port", 587);
        var enableSsl = configuration.GetValue("Email:Smtp:EnableSsl", true);
        var fromName = configuration["Email:Smtp:FromName"] ?? "MilanSetu";

        using var message = new MailMessage
        {
            From = new MailAddress(senderAddress.Address, fromName),
            Subject = "Your MilanSetu verification code",
            Body = $"Your MilanSetu verification code is {code}. It expires in 10 minutes. If you did not request this code, you can ignore this email.",
            IsBodyHtml = false
        };
        message.To.Add(recipient);

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = enableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(username, password)
        };

        await client.SendMailAsync(message, cancellationToken);
        logger.LogInformation("Email verification code sent to masked destination {Destination}.", MaskEmail(destination));
    }

    private async Task SendMobileAsync(string destination, string code, CancellationToken ct)
    {
        var accountSid = configuration["Sms:Twilio:AccountSid"];
        var authToken = configuration["Sms:Twilio:AuthToken"];
        var fromNumber = configuration["Sms:Twilio:FromNumber"];

        if (string.IsNullOrWhiteSpace(accountSid) || string.IsNullOrWhiteSpace(authToken) ||
            string.IsNullOrWhiteSpace(fromNumber))
        {
            if (environment.IsDevelopment())
            {
                logger.LogWarning(
                    "SMS delivery is not configured in Development; OTP was not logged for destination {Destination}.",
                    MaskPhone(destination));
                return;
            }

            throw new InvalidOperationException("SMS delivery is not configured for this environment.");
        }

        if (!E164Pattern.IsMatch(destination))
            throw new InvalidOperationException("The mobile number must use international E.164 format, for example +919876543210.");
        if (!E164Pattern.IsMatch(fromNumber))
            throw new InvalidOperationException("Sms:Twilio:FromNumber must use international E.164 format.");
        if (!accountSid.StartsWith("AC", StringComparison.Ordinal) ||
            accountSid.Length != 34 ||
            accountSid.Any(c => !Uri.IsHexDigit(c)))
            throw new InvalidOperationException("Sms:Twilio:AccountSid is invalid.");

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"https://api.twilio.com/2010-04-01/Accounts/{accountSid}/Messages.json");
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{accountSid}:{authToken}"));
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["To"] = destination,
            ["From"] = fromNumber,
            ["Body"] = $"Your MilanSetu verification code is {code}. It expires in 10 minutes. Do not share this code."
        });

        try
        {
            using var response = await httpClientFactory.CreateClient("twilio-sms").SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("SMS provider rejected verification delivery with status {StatusCode}.",
                    (int)response.StatusCode);
                throw new InvalidOperationException("SMS verification delivery failed. Please try again later.");
            }
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning("SMS provider request failed with exception type {ExceptionType}.", ex.GetType().Name);
            throw new InvalidOperationException("SMS verification delivery failed. Please try again later.");
        }

        logger.LogInformation("Mobile verification code sent to masked destination {Destination}.", MaskPhone(destination));
    }

    private static string MaskEmail(string value) => value.Contains('@') ? "***@***" : "***";

    private static string MaskPhone(string value)
    {
        var digits = new string(value.Where(char.IsDigit).ToArray());
        return digits.Length < 4 ? "***" : $"+***{digits[^4..]}";
    }
}

public sealed class VerificationChallengeService(IConfiguration configuration)
{
    private const int CodeLength = 6;
    private const int MaxAttempts = 5;
    private static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(10);

    public string GenerateCode() => RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

    public string HashCode(Guid userId, VerificationType type, string code)
    {
        var secret = configuration["Verification:HashKey"];
        if (string.IsNullOrWhiteSpace(secret) || Encoding.UTF8.GetByteCount(secret) < 32)
            throw new InvalidOperationException("Verification:HashKey must be configured with at least 32 bytes.");

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var input = $"{userId:N}|{type}|{code}";
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(input)));
    }

    public bool IsExpired(VerificationChallenge challenge, DateTimeOffset now) => now >= challenge.ExpiresAt;
    public bool HasTooManyAttempts(VerificationChallenge challenge) => challenge.FailedAttempts >= MaxAttempts;

    public bool IsValidCode(Guid userId, VerificationType type, VerificationChallenge challenge, string code)
    {
        if (code.Length != CodeLength || !code.All(char.IsDigit)) return false;
        var expected = HashCode(userId, type, code);
        return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(challenge.CodeHash));
    }

    public static DateTimeOffset GetExpiry(DateTimeOffset now) => now.Add(CodeLifetime);
    public static TimeSpan ResendCooldown => TimeSpan.FromSeconds(60);
}
