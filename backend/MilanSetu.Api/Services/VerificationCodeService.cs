using System.Security.Cryptography;
using System.Text;
using MilanSetu.Api.Domain;

namespace MilanSetu.Api.Services;

public interface IVerificationCodeSender
{
    Task SendAsync(VerificationType type, string destination, string code, CancellationToken cancellationToken);
}

public sealed class VerificationCodeSender(IHostEnvironment environment, ILogger<VerificationCodeSender> logger) : IVerificationCodeSender
{
    public Task SendAsync(VerificationType type, string destination, string code, CancellationToken cancellationToken)
    {
        // Real SMS/email providers are deliberately not hard-coded into the product.
        // Development can use the server log for local testing; production requires a configured provider.
        if (environment.IsDevelopment())
        {
            logger.LogInformation("Development verification code generated for {Type} to masked destination {Destination}: {Code}",
                type, Mask(destination), code);
            return Task.CompletedTask;
        }

        throw new InvalidOperationException("No verification delivery provider is configured for this environment.");
    }

    private static string Mask(string value)
    {
        if (value.Length <= 4) return "***";
        return $"{value[..Math.Min(2, value.Length)]}***{value[^2..]}";
    }
}

public sealed class VerificationChallengeService(IConfiguration configuration)
{
    private const int CodeLength = 6;
    private const int MaxAttempts = 5;
    private static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(10);

    public string GenerateCode()
    {
        return RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
    }

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
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(challenge.CodeHash));
    }

    public static DateTimeOffset GetExpiry(DateTimeOffset now) => now.Add(CodeLifetime);
    public static TimeSpan ResendCooldown => TimeSpan.FromSeconds(60);
}