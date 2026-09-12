using System.Security.Claims;
using MilanSetu.Api.Data;
using MilanSetu.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MilanSetu.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/verification")]
public sealed class VerificationController(MilanSetuDbContext db) : ControllerBase
{
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var existing = await db.VerificationRequests.AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.RequestedAt)
            .ToListAsync(ct);

        var result = Enum.GetValues<VerificationType>().Select(type =>
        {
            var item = existing.FirstOrDefault(x => x.Type == type);
            return new
            {
                type = type.ToString(),
                status = item?.Status.ToString() ?? VerificationStatus.NotStarted.ToString(),
                requestedAt = item?.RequestedAt,
                verifiedAt = item?.VerifiedAt
            };
        });

        return Ok(result);
    }

    [HttpPost("{type}/request")]
    public async Task<IActionResult> RequestVerification(VerificationType type, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (!Enum.IsDefined(type)) return BadRequest(new { message = "Unsupported verification type." });

        var latest = await db.VerificationRequests
            .Where(x => x.UserId == userId && x.Type == type)
            .OrderByDescending(x => x.RequestedAt)
            .FirstOrDefaultAsync(ct);

        if (latest?.Status == VerificationStatus.Verified)
            return Conflict(new { message = "This verification is already completed." });

        if (latest?.Status == VerificationStatus.Pending && latest.RequestedAt > DateTimeOffset.UtcNow.AddMinutes(-10))
            return Conflict(new { message = "A verification request is already pending." });

        var request = new VerificationRequest
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = type,
            Status = VerificationStatus.Pending,
            RequestedAt = DateTimeOffset.UtcNow
        };

        db.VerificationRequests.Add(request);
        await db.SaveChangesAsync(ct);

        return Accepted(new
        {
            type = request.Type.ToString(),
            status = request.Status.ToString(),
            message = type switch
            {
                VerificationType.Mobile => "Mobile verification is requested. OTP delivery will be enabled in the next verification step.",
                VerificationType.Email => "Email verification is requested. Verification email delivery will be enabled in the next verification step.",
                VerificationType.Identity => "Identity verification is queued for secure review.",
                VerificationType.Education => "Education verification is queued for secure review.",
                VerificationType.Employment => "Employment verification is queued for secure review.",
                _ => "Verification requested."
            }
        });
    }

    private bool TryGetUserId(out Guid userId) =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out userId);
}
