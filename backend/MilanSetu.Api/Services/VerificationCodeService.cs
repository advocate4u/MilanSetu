using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using MilanSetu.Api.Domain;

namespace MilanSetu.Api.Services;

public interface IVerificationCodeSender
{
    Task SendAsync(VerificationType type, string destination, string code, CancellationToken cancellationToken);
}

public sealed class VerificationCodeSender(
    IHostEnvironment environment,
    IConfiguration configuration,
    ILogger<VerificationCodeSender> logger) : IVerificationCodeSender
{
    public async Task SendAsync(VerificationType type, string destination, string code, CancellationToken cancellationToken)
    {
        if (type == VerificationType.Mobile)
        {
            // Mobile OTP delivery is intentionally left disabled until the mobile provider is selected.
            throw new InvalidOperationException("Mobile verification delivery is not configured yet.");
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
                logger.LogInformation(
                    "Development email verification code generated for masked destination {Destination}: {Code}",
                    Mask(destination), code);
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
        logger.LogInformation("Email verification code sent to masked destination {Destination}.", Mask(destination));
    }

    private static string Mask(string value)
    {
        var at = value.IndexOf('@');
        if (at <= 0) return "***";
        var local = value[..at];
        var domain = value[at..];
        var visible = local.Length <= 2 ? local[..1] : local[..2];
        return $"{visible}***{domain}";
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
