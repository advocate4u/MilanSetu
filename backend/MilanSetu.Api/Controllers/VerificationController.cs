using System.Security.Claims;
using MilanSetu.Api.Data;
using MilanSetu.Api.Domain;
using MilanSetu.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace MilanSetu.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/verification")]
public sealed class VerificationController(
    MilanSetuDbContext db,
    VerificationChallengeService challenges,
    IVerificationCodeSender sender) : ControllerBase
{
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var existing = await db.VerificationRequests.AsNoTracking().Where(x => x.UserId == userId).OrderByDescending(x => x.RequestedAt).ToListAsync(ct);
        var result = Enum.GetValues<VerificationType>().Select(type => { var item = existing.FirstOrDefault(x => x.Type == type); return new { type = type.ToString(), status = item?.Status.ToString() ?? VerificationStatus.NotStarted.ToString(), requestedAt = item?.RequestedAt, verifiedAt = item?.VerifiedAt }; });
        return Ok(result);
    }

    [HttpPost("{type}/request")]
    [EnableRateLimiting("verification")]
    public async Task<IActionResult> RequestVerification(VerificationType type, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (!Enum.IsDefined(type)) return BadRequest(new { message = "Unsupported verification type." });
        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == userId && x.IsActive, ct);
        if (user is null) return Unauthorized();
        var latest = await db.VerificationRequests.Where(x => x.UserId == userId && x.Type == type).OrderByDescending(x => x.RequestedAt).FirstOrDefaultAsync(ct);
        if (latest?.Status == VerificationStatus.Verified) return Conflict(new { message = "This verification is already completed." });

        if (type is VerificationType.Mobile or VerificationType.Email)
        {
            var destination = type == VerificationType.Mobile ? user.PhoneNumber : user.Email;
            if (string.IsNullOrWhiteSpace(destination)) return BadRequest(new { message = type == VerificationType.Mobile ? "Add a mobile number to your account before verification." : "Add an email address to your account before verification." });
            var alreadyVerified = type == VerificationType.Mobile ? user.IsPhoneVerified : user.IsEmailVerified;
            if (alreadyVerified) return Conflict(new { message = "This verification is already completed." });
            var recentChallenge = await db.VerificationChallenges.Where(x => x.UserId == userId && x.Type == type && x.ConsumedAt == null).OrderByDescending(x => x.CreatedAt).FirstOrDefaultAsync(ct);
            if (recentChallenge is not null && recentChallenge.CreatedAt > DateTimeOffset.UtcNow.Subtract(VerificationChallengeService.ResendCooldown)) return Conflict(new { message = "Please wait before requesting another verification code." });
            if (latest is null || latest.Status != VerificationStatus.Pending || latest.RequestedAt <= DateTimeOffset.UtcNow.AddMinutes(-10))
            {
                latest = new VerificationRequest { Id = Guid.NewGuid(), UserId = userId, Type = type, Status = VerificationStatus.Pending, RequestedAt = DateTimeOffset.UtcNow };
                db.VerificationRequests.Add(latest);
            }
            var code = challenges.GenerateCode();
            var challenge = new VerificationChallenge { Id = Guid.NewGuid(), UserId = userId, VerificationRequestId = latest.Id, Type = type, Destination = destination, CodeHash = challenges.HashCode(userId, type, code), CreatedAt = DateTimeOffset.UtcNow, ExpiresAt = VerificationChallengeService.GetExpiry(DateTimeOffset.UtcNow) };
            db.VerificationChallenges.Add(challenge);
            await db.SaveChangesAsync(ct);
            try { await sender.SendAsync(type, destination, code, ct); }
            catch (InvalidOperationException)
            {
                db.VerificationChallenges.Remove(challenge);
                await db.SaveChangesAsync(ct);
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Verification delivery is temporarily unavailable. Please try again later." });
            }
            return Accepted(new { type = type.ToString(), status = latest.Status.ToString(), expiresAt = challenge.ExpiresAt, resendAfterSeconds = (int)VerificationChallengeService.ResendCooldown.TotalSeconds, message = type == VerificationType.Mobile ? "A verification code was sent to your mobile number." : "A verification code was sent to your email address." });
        }

        if (latest?.Status == VerificationStatus.Pending && latest.RequestedAt > DateTimeOffset.UtcNow.AddMinutes(-10)) return Conflict(new { message = "A verification request is already pending." });
        var request = new VerificationRequest { Id = Guid.NewGuid(), UserId = userId, Type = type, Status = VerificationStatus.Pending, RequestedAt = DateTimeOffset.UtcNow };
        db.VerificationRequests.Add(request);
        await db.SaveChangesAsync(ct);
        return Accepted(new { type = request.Type.ToString(), status = request.Status.ToString(), message = type switch { VerificationType.Identity => "Identity verification is queued for secure review.", VerificationType.Education => "Education verification is queued for secure review.", VerificationType.Employment => "Employment verification is queued for secure review.", _ => "Verification requested." } });
    }

    [HttpPost("{type}/verify")]
    [EnableRateLimiting("verification")]
    public async Task<IActionResult> Verify(VerificationType type, [FromBody] VerifyCodeRequest input, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (type is not (VerificationType.Mobile or VerificationType.Email)) return BadRequest(new { message = "OTP verification is available for mobile and email only." });
        var code = input.Code?.Trim() ?? string.Empty;
        if (code.Length != 6 || !code.All(char.IsDigit)) return BadRequest(new { message = "Enter the 6-digit verification code." });
        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == userId && x.IsActive, ct);
        if (user is null) return Unauthorized();
        var challenge = await db.VerificationChallenges.Where(x => x.UserId == userId && x.Type == type && x.ConsumedAt == null).OrderByDescending(x => x.CreatedAt).FirstOrDefaultAsync(ct);
        if (challenge is null || challenges.IsExpired(challenge, DateTimeOffset.UtcNow) || challenges.HasTooManyAttempts(challenge)) return BadRequest(new { message = "This verification code is invalid or expired. Request a new code." });
        challenge.FailedAttempts++;
        challenge.LastAttemptAt = DateTimeOffset.UtcNow;
        if (!challenges.IsValidCode(userId, type, challenge, code))
        {
            await db.SaveChangesAsync(ct);
            return BadRequest(new { message = challenge.FailedAttempts >= 5 ? "Too many incorrect attempts. Request a new code." : "The verification code is incorrect." });
        }
        var request = await db.VerificationRequests.FirstOrDefaultAsync(x => x.Id == challenge.VerificationRequestId && x.UserId == userId && x.Type == type, ct);
        if (request is null) return BadRequest(new { message = "The verification request is no longer available. Request a new code." });
        challenge.ConsumedAt = DateTimeOffset.UtcNow;
        challenge.VerifiedAt = challenge.ConsumedAt;
        request.Status = VerificationStatus.Verified;
        request.VerifiedAt = challenge.VerifiedAt;
        request.ReviewedAt = challenge.VerifiedAt;
        if (type == VerificationType.Mobile) user.IsPhoneVerified = true; else user.IsEmailVerified = true;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(new { type = type.ToString(), status = VerificationStatus.Verified.ToString(), verifiedAt = request.VerifiedAt, message = type == VerificationType.Mobile ? "Mobile number verified successfully." : "Email address verified successfully." });
    }

    private bool TryGetUserId(out Guid userId) => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out userId);
}

public sealed record VerifyCodeRequest(string? Code);
