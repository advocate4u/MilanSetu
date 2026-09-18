using System.Text.Json;
using MilanSetu.Api.Data;
using MilanSetu.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MilanSetu.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/admin/platform-settings")]
public sealed class AdminPlatformSettingsController(MilanSetuDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        if (!await IsSuperAdmin(ct)) return Forbid();
        return Ok(await db.PlatformSettings.AsNoTracking().SingleOrDefaultAsync(x => x.Id == 1, ct) ?? new PlatformSettings());
    }

    [HttpPut]
    public async Task<IActionResult> Put(PlatformSettingsRequest request, CancellationToken ct)
    {
        if (!await IsSuperAdmin(ct)) return Forbid();
        if (request.MaxProfilePhotos is < 1 or > 20) return BadRequest(new { message = "Maximum profile photos must be between 1 and 20." });
        var s = await db.PlatformSettings.SingleOrDefaultAsync(x => x.Id == 1, ct);
        if (s is null) { s = new PlatformSettings(); db.PlatformSettings.Add(s); }
        s.MessagingEnabled = request.MessagingEnabled; s.MaxProfilePhotos = request.MaxProfilePhotos; s.UpdatedAt = DateTimeOffset.UtcNow; s.UpdatedByUserId = GetUserId();
        db.AuditLogs.Add(new AuditLog { Id = Guid.NewGuid(), ActorUserId = s.UpdatedByUserId, Action = "superadmin.platform-settings.update", ResourceType = "PlatformSettings", ResourceId = Guid.Empty, Metadata = JsonSerializer.Serialize(request) });
        await db.SaveChangesAsync(ct);
        return Ok(s);
    }
    private async Task<bool> IsSuperAdmin(CancellationToken ct) => TryGetUserId(out var id) && await db.UserRoleAssignments.AnyAsync(x => x.UserId == id && x.Role == UserRole.SuperAdmin, ct);
    private Guid? GetUserId() => Guid.TryParse(User.FindFirst("sub")?.Value, out var id) ? id : null;
    private bool TryGetUserId(out Guid id) => Guid.TryParse(User.FindFirst("sub")?.Value, out id);
}
public sealed record PlatformSettingsRequest(bool MessagingEnabled, int MaxProfilePhotos);