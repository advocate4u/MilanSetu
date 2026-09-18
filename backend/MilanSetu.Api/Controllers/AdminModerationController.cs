using System.Text.Json;
using MilanSetu.Api.Data;
using MilanSetu.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MilanSetu.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/admin/moderation")]
public sealed class AdminModerationController(MilanSetuDbContext db) : ControllerBase
{
    [HttpGet("cases")]
    public async Task<IActionResult> Cases(ModerationStatus? status, ModerationSeverity? severity, int page = 1, int pageSize = 25, CancellationToken ct = default)
    {
        var actor = await GetAdminId(ct);
        if (actor is null) return Forbid();

        page = Math.Clamp(page, 1, 10000);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.ModerationCases.AsNoTracking();
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);
        if (severity.HasValue) query = query.Where(x => x.Severity == severity.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.Severity).ThenBy(x => x.CreatedAt).ThenBy(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new {
                x.Id, x.ReportId, x.TargetUserId,
                severity = x.Severity.ToString(), status = x.Status.ToString(), action = x.Action.ToString(),
                x.CreatedAt, x.ReviewedAt,
                reason = x.Report.Reason.ToString(), x.Report.Details
            }).ToListAsync(ct);

        await Audit(actor.Value, "moderation.queue.view", null, null, ct);
        return Ok(new { page, pageSize, total, items });
    }

    [HttpGet("cases/{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var actor = await GetAdminId(ct);
        if (actor is null) return Forbid();

        var item = await db.ModerationCases.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new {
                x.Id, x.ReportId, x.TargetUserId,
                severity = x.Severity.ToString(), status = x.Status.ToString(), action = x.Action.ToString(),
                x.CreatedAt, x.ReviewedAt,
                report = new { x.Report.Reason, x.Report.Details, x.Report.ReporterUserId, x.Report.CreatedAt }
            }).SingleOrDefaultAsync(ct);

        if (item is null) return NotFound();
        await Audit(actor.Value, "moderation.case.view", id, null, ct);
        return Ok(item);
    }

    [HttpPost("cases/{id:guid}/decide")]
    public async Task<IActionResult> Decide(Guid id, ModerationDecisionRequest input, CancellationToken ct)
    {
        var actor = await GetAdminId(ct);
        if (actor is null) return Forbid();

        if (!Enum.IsDefined(input.Action)) return BadRequest(new { message = "Invalid moderation action." });
        if (!Enum.IsDefined(input.Status) || input.Status is not (ModerationStatus.Resolved or ModerationStatus.Dismissed))
            return BadRequest(new { message = "Status must be Resolved or Dismissed." });
        var notes = input.Notes?.Trim();
        if (notes?.Length > 2000) return BadRequest(new { message = "Moderation notes cannot exceed 2000 characters." });

        var item = await db.ModerationCases.Include(x => x.Report).SingleOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return NotFound();
        if (item.Status is ModerationStatus.Resolved or ModerationStatus.Dismissed)
            return Conflict(new { message = "This moderation case has already been decided." });

        item.Status = input.Status;
        item.Action = input.Action;
        item.ReviewedAt = DateTimeOffset.UtcNow;

        item.Report.Status = input.Status == ModerationStatus.Resolved ? ReportStatus.Resolved : ReportStatus.Dismissed;
        item.Report.ResolvedAt = item.ReviewedAt;

        if (input.Action is ModerationAction.TemporarySuspension or ModerationAction.PermanentBan)
        {
            var target = await db.Users.SingleOrDefaultAsync(x => x.Id == item.TargetUserId, ct);
            if (target is null) return NotFound(new { message = "Target user no longer exists." });
            target.IsActive = false;
            target.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(ct);
        await Audit(actor.Value, "moderation.case.decide", id, new { input.Status, input.Action, notes }, ct);
        return Ok(new { id, status = item.Status.ToString(), action = item.Action.ToString(), reviewedAt = item.ReviewedAt });
    }

    private async Task<Guid?> GetAdminId(CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var userId)) return null;
        var role = await db.UserRoleAssignments.AsNoTracking()
            .Where(x => x.UserId == userId).Select(x => (UserRole?)x.Role).SingleOrDefaultAsync(ct);
        return role == UserRole.Admin ? userId : null;
    }

    private async Task Audit(Guid actor, string action, Guid? resourceId, object? metadata, CancellationToken ct)
    {
        db.AuditLogs.Add(new AuditLog {
            Id = Guid.NewGuid(), ActorUserId = actor, Action = action,
            ResourceType = "ModerationCase", ResourceId = resourceId,
            Metadata = metadata is null ? null : JsonSerializer.Serialize(metadata),
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(ct);
    }
}

public sealed record ModerationDecisionRequest(ModerationStatus Status, ModerationAction Action, string? Notes);
