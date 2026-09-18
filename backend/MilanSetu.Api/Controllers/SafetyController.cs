using MilanSetu.Api.Data;
using MilanSetu.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MilanSetu.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/safety")]
public sealed class SafetyController(MilanSetuDbContext db) : ControllerBase
{
    [HttpGet("me")]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var openReports = await db.UserReports.AsNoTracking().CountAsync(x => x.ReporterUserId == userId && (x.Status == ReportStatus.Open || x.Status == ReportStatus.Reviewing), ct);
        var blocked = await db.Blocks.AsNoTracking().CountAsync(x => x.BlockerUserId == userId, ct);
        var unread = await db.Notifications.AsNoTracking().CountAsync(x => x.UserId == userId && x.ReadAt == null, ct);
        var recentReports = await db.UserReports.AsNoTracking().CountAsync(x => x.ReporterUserId == userId && x.CreatedAt >= DateTimeOffset.UtcNow.AddDays(-30), ct);
        return Ok(new
        {
            openReports,
            blockedProfiles = blocked,
            unreadSafetyNotifications = unread,
            reportsLast30Days = recentReports,
            protections = new[] { "Contact details stay private.", "Blocked profiles cannot message you.", "Reports are reviewed by authorized staff." }
        });
    }

    private bool TryGetUserId(out Guid userId) => Guid.TryParse(User.FindFirst("sub")?.Value, out userId);
}