using System.Text.Json;
using MilanSetu.Api.Data;
using MilanSetu.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MilanSetu.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/contact-sharing")]
public sealed class ContactSharingController(MilanSetuDbContext db) : ControllerBase
{
    [HttpGet("settings")]
    public async Task<IActionResult> Settings(CancellationToken ct)
    {
        var s = await GetSettings(ct);
        return Ok(new { enabled = s.Enabled, requireMutualMatch = s.RequireMutualMatch, sharePhone = s.SharePhone, shareEmail = s.ShareEmail, shareWhatsApp = s.ShareWhatsApp });
    }

    [HttpGet("requests")]
    public async Task<IActionResult> Requests(CancellationToken ct)
    {
        if (!TryGetUserId(out var uid)) return Unauthorized();
        var incoming = await db.ContactShareRequests.AsNoTracking().Where(x => x.RecipientUserId == uid).OrderByDescending(x => x.CreatedAt)
            .Select(x => new { x.Id, x.RequesterUserId, status = x.Status.ToString(), x.CreatedAt, x.RespondedAt }).ToListAsync(ct);
        var outgoing = await db.ContactShareRequests.AsNoTracking().Where(x => x.RequesterUserId == uid).OrderByDescending(x => x.CreatedAt)
            .Select(x => new { x.Id, x.RecipientUserId, status = x.Status.ToString(), x.CreatedAt, x.RespondedAt }).ToListAsync(ct);
        return Ok(new { incoming, outgoing });
    }

    [HttpPost("request/{otherUserId:guid}")]
    public async Task<IActionResult> Request(Guid otherUserId, CancellationToken ct)
    {
        if (!TryGetUserId(out var uid)) return Unauthorized();
        if (uid == otherUserId) return BadRequest(new { message = "You cannot request your own contact details." });
        var s = await GetSettings(ct);
        if (!s.Enabled) return Conflict(new { message = "Contact sharing is currently disabled." });
        var connection = await FindConnection(uid, otherUserId, ct);
        if (connection is null) return Conflict(new { message = "Contact sharing is available only after the match request has been accepted." });
        if (await IsBlocked(uid, otherUserId, ct)) return Conflict(new { message = "This connection is unavailable." });

        var r = await db.ContactShareRequests.SingleOrDefaultAsync(x => x.ConnectionId == connection.Id && x.RequesterUserId == uid && x.RecipientUserId == otherUserId, ct);
        if (r is not null && r.Status == ContactShareRequestStatus.Accepted) return Ok(new { id = r.Id, status = "Accepted" });
        if (r is not null && r.Status == ContactShareRequestStatus.Pending) return Conflict(new { message = "Contact share request is already pending." });
        if (r is null) { r = new ContactShareRequest { Id = Guid.NewGuid(), ConnectionId = connection.Id, RequesterUserId = uid, RecipientUserId = otherUserId }; db.ContactShareRequests.Add(r); }
        else { r.Status = ContactShareRequestStatus.Pending; r.RespondedAt = null; }
        db.AuditLogs.Add(new AuditLog { Id = Guid.NewGuid(), ActorUserId = uid, Action = "contact-share.request", ResourceType = "ContactShareRequest", ResourceId = r.Id });
        await db.SaveChangesAsync(ct);
        return Ok(new { id = r.Id, status = r.Status.ToString() });
    }

    [HttpPost("requests/{id:guid}/accept")]
    public Task<IActionResult> Accept(Guid id, CancellationToken ct) => Respond(id, ContactShareRequestStatus.Accepted, ct);

    [HttpPost("requests/{id:guid}/decline")]
    public Task<IActionResult> Decline(Guid id, CancellationToken ct) => Respond(id, ContactShareRequestStatus.Declined, ct);

    [HttpGet("with/{otherUserId:guid}")]
    public async Task<IActionResult> With(Guid otherUserId, CancellationToken ct)
    {
        if (!TryGetUserId(out var uid)) return Unauthorized();
        var s = await GetSettings(ct);
        if (!s.Enabled) return Ok(new { visible = false, reason = "disabled" });
        var connection = await FindConnection(uid, otherUserId, ct);
        if (connection is null || await IsBlocked(uid, otherUserId, ct)) return Ok(new { visible = false, reason = "match-required" });

        var accepted = await db.ContactShareRequests.AsNoTracking().Where(x => x.ConnectionId == connection.Id && x.Status == ContactShareRequestStatus.Accepted &&
            ((x.RequesterUserId == uid && x.RecipientUserId == otherUserId) || (x.RequesterUserId == otherUserId && x.RecipientUserId == uid))).CountAsync(ct);
        if (s.RequireMutualMatch ? accepted < 2 : accepted < 1)
            return Ok(new { visible = false, reason = s.RequireMutualMatch ? "mutual-consent-required" : "consent-required" });

        var other = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == otherUserId, ct);
        if (other is null) return NotFound();
        return Ok(new { visible = true, phone = s.SharePhone ? other.PhoneNumber : null, whatsApp = s.ShareWhatsApp ? other.PhoneNumber : null, email = s.ShareEmail ? other.Email : null });
    }

    private async Task<IActionResult> Respond(Guid id, ContactShareRequestStatus status, CancellationToken ct)
    {
        if (!TryGetUserId(out var uid)) return Unauthorized();
        var r = await db.ContactShareRequests.SingleOrDefaultAsync(x => x.Id == id && x.RecipientUserId == uid, ct);
        if (r is null) return NotFound();
        if (r.Status != ContactShareRequestStatus.Pending) return Conflict(new { message = "Request is no longer pending." });
        if (await IsBlocked(uid, r.RequesterUserId, ct)) return Conflict(new { message = "This connection is unavailable." });
        if (!(await GetSettings(ct)).Enabled) return Conflict(new { message = "Contact sharing is currently disabled." });
        r.Status = status; r.RespondedAt = DateTimeOffset.UtcNow;
        db.AuditLogs.Add(new AuditLog { Id = Guid.NewGuid(), ActorUserId = uid, Action = "contact-share." + status.ToString().ToLowerInvariant(), ResourceType = "ContactShareRequest", ResourceId = id });
        await db.SaveChangesAsync(ct);
        return Ok(new { status = status.ToString() });
    }

    private async Task<ContactSharingSettings> GetSettings(CancellationToken ct)
    {
        var s = await db.ContactSharingSettings.SingleOrDefaultAsync(x => x.Id == 1, ct);
        if (s is null) { s = new ContactSharingSettings(); db.ContactSharingSettings.Add(s); await db.SaveChangesAsync(ct); }
        return s;
    }
    private Task<Connection?> FindConnection(Guid a, Guid b, CancellationToken ct) { var x = a.CompareTo(b) < 0 ? a : b; var y = a.CompareTo(b) < 0 ? b : a; return db.Connections.SingleOrDefaultAsync(c => c.UserAId == x && c.UserBId == y, ct); }
    private Task<bool> IsBlocked(Guid a, Guid b, CancellationToken ct) => db.Blocks.AnyAsync(x => (x.BlockerUserId == a && x.BlockedUserId == b) || (x.BlockerUserId == b && x.BlockedUserId == a), ct);
    private bool TryGetUserId(out Guid id) => Guid.TryParse(User.FindFirst("sub")?.Value, out id);
}

[ApiController]
[Authorize]
[Route("api/admin/contact-sharing")]
public sealed class AdminContactSharingController(MilanSetuDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct) { if (!await IsSuperAdmin(ct)) return Forbid(); return Ok(await db.ContactSharingSettings.AsNoTracking().SingleOrDefaultAsync(x => x.Id == 1, ct) ?? new ContactSharingSettings()); }

    [HttpPut]
    public async Task<IActionResult> Put(ContactSharingSettingsRequest request, CancellationToken ct)
    {
        if (!await IsSuperAdmin(ct)) return Forbid();
        var s = await db.ContactSharingSettings.SingleOrDefaultAsync(x => x.Id == 1, ct);
        if (s is null) { s = new ContactSharingSettings(); db.ContactSharingSettings.Add(s); }
        s.Enabled = request.Enabled; s.RequireMutualMatch = request.RequireMutualMatch; s.SharePhone = request.SharePhone; s.ShareEmail = request.ShareEmail; s.ShareWhatsApp = request.ShareWhatsApp; s.UpdatedAt = DateTimeOffset.UtcNow; s.UpdatedByUserId = GetUserId();
        db.AuditLogs.Add(new AuditLog { Id = Guid.NewGuid(), ActorUserId = s.UpdatedByUserId, Action = "superadmin.contact-sharing.update", ResourceType = "ContactSharingSettings", ResourceId = Guid.Empty, Metadata = JsonSerializer.Serialize(request) });
        await db.SaveChangesAsync(ct); return Ok(s);
    }
    private async Task<bool> IsSuperAdmin(CancellationToken ct) => TryGetUserId(out var id) && await db.UserRoleAssignments.AnyAsync(x => x.UserId == id && x.Role == UserRole.SuperAdmin, ct);
    private Guid? GetUserId() => Guid.TryParse(User.FindFirst("sub")?.Value, out var id) ? id : null;
    private bool TryGetUserId(out Guid id) => Guid.TryParse(User.FindFirst("sub")?.Value, out id);
}
public sealed record ContactSharingSettingsRequest(bool Enabled, bool RequireMutualMatch, bool SharePhone, bool ShareEmail, bool ShareWhatsApp);