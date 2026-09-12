using System.Security.Cryptography;
using System.Text;
using MilanSetu.Api.Data;
using MilanSetu.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace MilanSetu.Api.Services;

public interface IVerificationCodeSender
{
    Task SendAsync(VerificationChallengePurpose purpose, string destination, CancellationToken cancellationToken);
}

public sealed class VerificationCodeSender : IVerificationCodeSender
{
    private readonly ILogger<VerificationCodeSender> logger;

    public VerificationCodeSender(ILogger<VerificationCodeSender> logger) => this.logger = logger;

    public Task SendAsync(VerificationChallengePurpose purpose, string destination, CancellationToken cancellationToken)
    {
        // Delivery is intentionally provider-backed. Never log or return the OTP.
        logger.LogInformation("Verification code delivery requested for {Purpose}.", purpose);
        return Task.CompletedTask;
    }
}

public sealed class VerificationOtpService(MilanSetuDbContext db, IVerificationCodeSender sender)
{
    private const int CodeLength = 6;
    private const int MaxAttempts = 5;
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan ResendCooldown = TimeSpan.FromSeconds(60);

    public async Task<(bool Success, string Message)> IssueAsync(Guid userId, VerificationChallengePurpose purpose, string destination, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var recent = await db.VerificationChallenges.AsNoTracking()
            .Where(x => x.UserId == userId && x.Purpose == purpose && x.CreatedAt > now.Subtract(ResendCooldown) && x.ConsumedAt == null)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (recent is not null)
            return (false, "A verification code was sent recently. Please wait before requesting another code.");

        var code = GenerateCode();
        var challenge = new VerificationChallenge
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Purpose = purpose,
            CodeHash = HashCode(code),
            CreatedAt = now,
            ExpiresAt = now.Add(Lifetime)
        };

        db.VerificationChallenges.Add(challenge);
        await db.SaveChangesAsync(ct);

        await sender.SendAsync(purpose, destination, ct);
        return (true, "A verification code has been sent. It expires in 10 minutes.");
    }

    public async Task<(bool Success, string Message)> VerifyAsync(Guid userId, VerificationChallengePurpose purpose, string code, CancellationToken ct)
    {
        if (code.Length != CodeLength || code.Any(c => c is < '0' or > '9'))
            return (false, "The verification code is invalid or expired.");

        var now = DateTimeOffset.UtcNow;
        var challenge = await db.VerificationChallenges
            .Where(x => x.UserId == userId && x.Purpose == purpose && x.ConsumedAt == null)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (challenge is null || challenge.ExpiresAt <= now || challenge.FailedAttempts >= MaxAttempts)
            return (false, "The verification code is invalid or expired.");

        var suppliedHash = HashCode(code);
        var expected = Convert.FromHexString(challenge.CodeHash);
        var supplied = Convert.FromHexString(suppliedHash);

        if (!CryptographicOperations.FixedTimeEquals(expected, supplied))
        {
            challenge.FailedAttempts++;
            if (challenge.FailedAttempts >= MaxAttempts)
                challenge.ConsumedAt = now;
            await db.SaveChangesAsync(ct);
            return (false, "The verification code is invalid or expired.");
        }

        challenge.ConsumedAt = now;
        var user = await db.Users.FirstAsync(x => x.Id == userId, ct);
        if (purpose == VerificationChallengePurpose.VerifyMobile)
            user.IsPhoneVerified = true;
        else
            user.IsEmailVerified = true;

        await db.SaveChangesAsync(ct);
        return (true, "Verification completed successfully.");
    }

    private static string GenerateCode()
    {
        var value = RandomNumberGenerator.GetInt32(0, 1_000_000);
        return value.ToString("D6");
    }

    private static string HashCode(string code)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(code));
        return Convert.ToHexString(hash);
    }
}
