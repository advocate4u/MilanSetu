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
[Route("api/verification-otp")]
public sealed class VerificationOtpController(MilanSetuDbContext db, VerificationOtpService otpService) : ControllerBase
{
    [HttpPost("mobile/send-code")]
    [EnableRateLimiting("verification")]
    public Task<IActionResult> SendMobileCode(CancellationToken ct) => SendCode(VerificationChallengePurpose.VerifyMobile, VerificationType.Mobile, ct);

    [HttpPost("email/send-code")]
    [EnableRateLimiting("verification")]
    public Task<IActionResult> SendEmailCode(CancellationToken ct) => SendCode(VerificationChallengePurpose.VerifyEmail, VerificationType.Email, ct);

    [HttpPost("mobile/verify")]
    [EnableRateLimiting("verification")]
    public Task<IActionResult> VerifyMobile([FromBody] VerifyCodeRequest request, CancellationToken ct) => VerifyCode(VerificationChallengePurpose.VerifyMobile, request, ct);

    [HttpPost("email/verify")]
    [EnableRateLimiting("verification")]
    public Task<IActionResult> VerifyEmail([FromBody] VerifyCodeRequest request, CancellationToken ct) => VerifyCode(VerificationChallengePurpose.VerifyEmail, request, ct);

    private async Task<IActionResult> SendCode(VerificationChallengePurpose purpose, VerificationType type, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId && x.IsActive, ct);
        if (user is null) return Unauthorized();
        var destination = purpose == VerificationChallengePurpose.VerifyMobile ? user.PhoneNumber : user.Email;
        if (string.IsNullOrWhiteSpace(destination)) return BadRequest(new { message = purpose == VerificationChallengePurpose.VerifyMobile ? "Add a mobile number before verification." : "Add an email address before verification." });
        var alreadyVerified = purpose == VerificationChallengePurpose.VerifyMobile ? user.IsPhoneVerified : user.IsEmailVerified;
        if (alreadyVerified) return Conflict(new { message = "This verification is already completed." });
        await EnsureRequestAsync(userId, type, ct);
        try
        {
            var result = await otpService.IssueAsync(userId, purpose, destination, ct);
            return result.Success ? Accepted(new { message = result.Message }) : Conflict(new { message = result.Message });
        }
        catch (VerificationCodeDeliveryNotConfiguredException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Verification delivery is temporarily unavailable. Please try again later." });
        }
    }

    private async Task<IActionResult> VerifyCode(VerificationChallengePurpose purpose, VerifyCodeRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (request is null || string.IsNullOrWhiteSpace(request.Code)) return BadRequest(new { message = "Enter the verification code." });
        var result = await otpService.VerifyAsync(userId, purpose, request.Code.Trim(), ct);
        return result.Success ? Ok(new { message = result.Message }) : BadRequest(new { message = result.Message });
    }

    private async Task EnsureRequestAsync(Guid userId, VerificationType type, CancellationToken ct)
    {
        var latest = await db.VerificationRequests.Where(x => x.UserId == userId && x.Type == type).OrderByDescending(x => x.RequestedAt).FirstOrDefaultAsync(ct);
        if (latest?.Status == VerificationStatus.Verified) return;
        if (latest?.Status == VerificationStatus.Pending && latest.RequestedAt > DateTimeOffset.UtcNow.AddMinutes(-10)) return;
        db.VerificationRequests.Add(new VerificationRequest { Id = Guid.NewGuid(), UserId = userId, Type = type, Status = VerificationStatus.Pending, RequestedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync(ct);
    }

    private bool TryGetUserId(out Guid userId) => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out userId);
    public sealed record VerifyCodeRequest(string Code);
}