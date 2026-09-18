using MilanSetu.Api.Data;
using MilanSetu.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MilanSetu.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/privacy")]
public sealed class PrivacyController(MilanSetuDbContext db) : ControllerBase
{
    [HttpGet("me")]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var profile = await db.Profiles.AsNoTracking().SingleOrDefaultAsync(x => x.UserId == userId, ct);
        if (profile is null) return NotFound(new { message = "Create your profile first." });
        var blocked = await db.Blocks.AsNoTracking().CountAsync(x => x.BlockerUserId == userId, ct);
        return Ok(new
        {
            visibility = profile.Visibility.ToString(),
            discoverable = profile.Visibility != ProfileVisibility.Hidden,
            blockedProfiles = blocked,
            contactDetailsPublic = false,
            verificationDocumentsPublic = false
        });
    }

    [HttpPut("me")]
    public async Task<IActionResult> Update(PrivacyRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (!Enum.IsDefined(request.Visibility)) return BadRequest(new { message = "Invalid profile visibility." });
        var profile = await db.Profiles.SingleOrDefaultAsync(x => x.UserId == userId, ct);
        if (profile is null) return NotFound(new { message = "Create your profile first." });
        profile.Visibility = request.Visibility;
        profile.UpdatedAt = DateTimeOffset.UtcNow;
        db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(), ActorUserId = userId, Action = "privacy.visibility.update",
            ResourceType = "profile", ResourceId = profile.Id, Metadata = $"visibility={request.Visibility}"
        });
        await db.SaveChangesAsync(ct);
        return Ok(new { visibility = profile.Visibility.ToString(), discoverable = profile.Visibility != ProfileVisibility.Hidden });
    }

    private bool TryGetUserId(out Guid userId) => Guid.TryParse(User.FindFirst("sub")?.Value, out userId);
}

public sealed record PrivacyRequest(ProfileVisibility Visibility);