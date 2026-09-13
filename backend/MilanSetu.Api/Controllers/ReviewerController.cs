using System.Security.Claims;
using System.Text.Json;
using MilanSetu.Api.Data;
using MilanSetu.Api.Domain;
using MilanSetu.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MilanSetu.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/reviewer/verifications")]
public sealed class ReviewerController(MilanSetuDbContext db, ReviewerAuthorizationService authorization) : ControllerBase
{
    private static readonly VerificationType[] ReviewableTypes = [VerificationType.Identity, VerificationType.Education, VerificationType.Employment];

    [HttpGet]
    public async Task<IActionResult> Queue(VerificationType? type, VerificationStatus? status, int page = 1, int pageSize = 25, CancellationToken ct = default)
    {
        var actor = await authorization.GetAuthorizedActorAsync(User, ct); if (actor is null) return Forbid();
        page = Math.Clamp(page, 1, 10000); pageSize = Math.Clamp(pageSize, 1, 100);
        var query = db.VerificationRequests.AsNoTracking().Where(x => ReviewableTypes.Contains(x.Type));
        if (type.HasValue) query = query.Where(x => x.Type == type.Value); else if (!status.HasValue) query = query.Where(x => x.Status == VerificationStatus.Pending);
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);
        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(x => x.RequestedAt).ThenBy(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize).Select(x => new { x.Id, x.UserId, type = x.Type.ToString(), status = x.Status.ToString(), x.RequestedAt, x.ReviewedAt, x.ReviewedByUserId, x.ClaimedByUserId, x.ClaimedAt }).ToListAsync(ct);
        await WriteAudit(actor.Value.UserId, "verification.queue.view", "VerificationRequest", null, null, ct); return Ok(new { page, pageSize, total, items });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var actor = await authorization.GetAuthorizedActorAsync(User, ct); if (actor is null) return Forbid();
        var item = await db.VerificationRequests.AsNoTracking().Where(x => x.Id == id && ReviewableTypes.Contains(x.Type)).Select(x => new { x.Id, x.UserId, type = x.Type.ToString(), status = x.Status.ToString(), x.RequestedAt, x.ReviewedAt, x.VerifiedAt, x.ReviewedByUserId, x.ClaimedByUserId, x.ClaimedAt, x.ReviewerNotes }).SingleOrDefaultAsync(ct);
        if (item is null) return NotFound();
        await WriteAudit(actor.Value.UserId, "verification.detail.view", "VerificationRequest", id, null, ct); return Ok(item);
    }

    [HttpPost("{id:guid}/claim")]
    public async Task<IActionResult> Claim(Guid id, CancellationToken ct)
    {
        var actor = await authorization.GetAuthorizedActorAsync(User, ct); if (actor is null) return Forbid();
        var item = await db.VerificationRequests.SingleOrDefaultAsync(x => x.Id == id && ReviewableTypes.Contains(x.Type), ct); if (item is null) return NotFound();
        if (item.Status != VerificationStatus.Pending) return Conflict(new { message = "Only pending verification requests can be claimed." });
        if (item.ClaimedByUserId.HasValue && item.ClaimedByUserId != actor.Value.UserId && item.ClaimedAt > DateTimeOffset.UtcNow.AddMinutes(-30)) return Conflict(new { message = "This verification is currently being reviewed by another reviewer." });
        item.ClaimedByUserId = actor.Value.UserId; item.ClaimedAt = DateTimeOffset.UtcNow; await db.SaveChangesAsync(ct);
        await WriteAudit(actor.Value.UserId, "verification.claim", "VerificationRequest", id, null, ct); return Ok(new { id, claimedByUserId = actor.Value.UserId, claimedAt = item.ClaimedAt });
    }

    [HttpPost("{id:guid}/approve")]
    public Task<IActionResult> Approve(Guid id, DecisionRequest input, CancellationToken ct) => Decide(id, VerificationStatus.Verified, input, ct);

    [HttpPost("{id:guid}/reject")]
    public Task<IActionResult> Reject(Guid id, DecisionRequest input, CancellationToken ct) => Decide(id, VerificationStatus.Rejected, input, ct);

    private async Task<IActionResult> Decide(Guid id, VerificationStatus decision, DecisionRequest input, CancellationToken ct)
    {
        var actor = await authorization.GetAuthorizedActorAsync(User, ct); if (actor is null) return Forbid();
        var item = await db.VerificationRequests.SingleOrDefaultAsync(x => x.Id == id && ReviewableTypes.Contains(x.Type), ct); if (item is null) return NotFound();
        if (item.Status != VerificationStatus.Pending) return Conflict(new { message = "This verification has already been decided." });
        if (item.ClaimedByUserId.HasValue && item.ClaimedByUserId != actor.Value.UserId && item.ClaimedAt > DateTimeOffset.UtcNow.AddMinutes(-30)) return Conflict(new { message = "This verification is currently assigned to another reviewer." });
        var notes = input.Notes?.Trim(); if (decision == VerificationStatus.Rejected && string.IsNullOrWhiteSpace(notes)) return BadRequest(new { message = "A rejection reason is required." });
        if (notes?.Length > 2000) return BadRequest(new { message = "Reviewer notes cannot exceed 2000 characters." });
        item.Status = decision; item.ReviewedAt = DateTimeOffset.UtcNow; item.VerifiedAt = decision == VerificationStatus.Verified ? item.ReviewedAt : null; item.ReviewedByUserId = actor.Value.UserId; item.ReviewerNotes = notes; item.ClaimedByUserId = null; item.ClaimedAt = null;
        await db.SaveChangesAsync(ct); await WriteAudit(actor.Value.UserId, decision == VerificationStatus.Verified ? "verification.approve" : "verification.reject", "VerificationRequest", id, notes, ct);
        return Ok(new { id, status = decision.ToString(), reviewedAt = item.ReviewedAt });
    }

    private async Task WriteAudit(Guid actorUserId, string action, string resourceType, Guid? resourceId, string? notes, CancellationToken ct)
    {
        db.AuditLogs.Add(new AuditLog { Id = Guid.NewGuid(), ActorUserId = actorUserId, Action = action, ResourceType = resourceType, ResourceId = resourceId, Metadata = notes is null ? null : JsonSerializer.Serialize(new { notes }), CreatedAt = DateTimeOffset.UtcNow }); await db.SaveChangesAsync(ct);
    }
}

public sealed record DecisionRequest(string? Notes);
