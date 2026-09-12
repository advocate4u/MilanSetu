using System.Security.Cryptography;
using System.Text;
using MilanSetu.Api.Data;
using MilanSetu.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace MilanSetu.Api.Services;

public interface IVerificationCodeSender
{
    Task SendAsync(VerificationChallengePurpose purpose, string destination, string code, CancellationToken cancellationToken);
}

public sealed class VerificationCodeDeliveryNotConfiguredException : Exception
{
    public VerificationCodeDeliveryNotConfiguredException() : base("Verification code delivery is not configured.") { }
}

public sealed class VerificationCodeSender : IVerificationCodeSender
{
    public Task SendAsync(VerificationChallengePurpose purpose, string destination, string code, CancellationToken cancellationToken) =>
        throw new VerificationCodeDeliveryNotConfiguredException();
}

public sealed class VerificationOtpService(MilanSetuDbContext db, IVerificationCodeSender sender)
{
    public const int CodeLength = 6;
    public const int MaxAttempts = 5;
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan ResendCooldown = TimeSpan.FromSeconds(60);

    public async Task<(bool Success, string Message)> IssueAsync(Guid userId, VerificationChallengePurpose purpose, string destination, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var recent = await db.VerificationChallenges.AsNoTracking()
            .Where(x => x.UserId == userId && x.Purpose == purpose && x.CreatedAt > now.Subtract(ResendCooldown) && x.ConsumedAt == null)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(ct);
        if (recent is not null) return (false, "A verification code was sent recently. Please wait before requesting another code.");

        var code = GenerateCode();
        db.VerificationChallenges.Add(new VerificationChallenge
        {
            Id = Guid.NewGuid(), UserId = userId, Purpose = purpose,
            CodeHash = HashCode(code), CreatedAt = now, ExpiresAt = now.Add(Lifetime)
        });
        await db.SaveChangesAsync(ct);
        try { await sender.SendAsync(purpose, destination, code, ct); }
        catch
        {
            var challenge = await db.VerificationChallenges.Where(x => x.UserId == userId && x.Purpose == purpose && x.ConsumedAt == null)
                .OrderByDescending(x => x.CreatedAt).FirstOrDefaultAsync(ct);
            if (challenge is not null) db.VerificationChallenges.Remove(challenge);
            await db.SaveChangesAsync(ct);
            throw;
        }
        return (true, "A verification code has been sent. It expires in 10 minutes.");
    }

    public async Task<(bool Success, string Message)> VerifyAsync(Guid userId, VerificationChallengePurpose purpose, string code, CancellationToken ct)
    {
        if (code.Length != CodeLength || code.Any(c => c is < '0' or > '9')) return (false, "The verification code is invalid or expired.");
        var now = DateTimeOffset.UtcNow;
        var challenge = await db.VerificationChallenges.Where(x => x.UserId == userId && x.Purpose == purpose && x.ConsumedAt == null)
            .OrderByDescending(x => x.CreatedAt).FirstOrDefaultAsync(ct);
        if (challenge is null || challenge.ExpiresAt <= now || challenge.FailedAttempts >= MaxAttempts)
            return (false, "The verification code is invalid or expired.");

        var expected = Convert.FromHexString(challenge.CodeHash);
        var supplied = Convert.FromHexString(HashCode(code));
        if (!CryptographicOperations.FixedTimeEquals(expected, supplied))
        {
            challenge.FailedAttempts++;
            if (challenge.FailedAttempts >= MaxAttempts) challenge.ConsumedAt = now;
            await db.SaveChangesAsync(ct);
            return (false, "The verification code is invalid or expired.");
        }

        challenge.ConsumedAt = now;
        var user = await db.Users.FirstAsync(x => x.Id == userId, ct);
        var type = purpose == VerificationChallengePurpose.VerifyMobile ? VerificationType.Mobile : VerificationType.Email;
        var request = await db.VerificationRequests.Where(x => x.UserId == userId && x.Type == type)
            .OrderByDescending(x => x.RequestedAt).FirstOrDefaultAsync(ct);
        if (purpose == VerificationChallengePurpose.VerifyMobile) user.IsPhoneVerified = true; else user.IsEmailVerified = true;
        if (request is not null) { request.Status = VerificationStatus.Verified; request.VerifiedAt = now; }
        await db.SaveChangesAsync(ct);
        return (true, "Verification completed successfully.");
    }

    private static string GenerateCode() => RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
    private static string HashCode(string code) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code)));
}