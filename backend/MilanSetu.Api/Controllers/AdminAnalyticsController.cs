using MilanSetu.Api.Data;
using MilanSetu.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MilanSetu.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/admin/analytics")]
public sealed class AdminAnalyticsController(MilanSetuDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int days = 30, CancellationToken ct = default)
    {
        if (!await IsAdmin(ct)) return Forbid();
        days = Math.Clamp(days, 7, 90);
        var now = DateTimeOffset.UtcNow;
        var start = now.Date.AddDays(-(days - 1));

        var users = await db.Users.AsNoTracking().Where(x => x.CreatedAt >= start).Select(x => x.CreatedAt.Date).ToListAsync(ct);
        var messages = await db.Messages.AsNoTracking().Where(x => x.CreatedAt >= start).Select(x => x.CreatedAt.Date).ToListAsync(ct);
        var reports = await db.UserReports.AsNoTracking().Where(x => x.CreatedAt >= start).Select(x => new { x.CreatedAt, x.Reason }).ToListAsync(ct);
        var connections = await db.Connections.AsNoTracking().Select(x => x.Id).CountAsync(ct);
        var openCases = await db.ModerationCases.AsNoTracking().CountAsync(x => x.Status == ModerationStatus.Open || x.Status == ModerationStatus.Reviewing, ct);

        var daily = Enumerable.Range(0, days).Select(i =>
        {
            var date = start.AddDays(i);
            return new
            {
                date = DateOnly.FromDateTime(date).ToString("yyyy-MM-dd"),
                newUsers = users.Count(x => x.Date == date),
                messages = messages.Count(x => x.Date == date),
                reports = reports.Count(x => x.CreatedAt.Date == date)
            };
        }).ToList();

        var reportReasons = reports.GroupBy(x => x.Reason.ToString()).OrderByDescending(x => x.Count()).Select(x => new { reason = x.Key, count = x.Count() });
        return Ok(new { generatedAt = now, days, daily, totalConnections = connections, openModerationCases = openCases, reportReasons });
    }

    private async Task<bool> IsAdmin(CancellationToken ct) =>
        Guid.TryParse(User.FindFirst("sub")?.Value, out var id) &&
        await db.UserRoleAssignments.AsNoTracking().AnyAsync(x => x.UserId == id && x.Role == UserRole.Admin, ct);
}