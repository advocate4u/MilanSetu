using System.Security.Claims;
using MilanSetu.Api.Data;
using MilanSetu.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MilanSetu.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/reviewer/verifications")]
public sealed class ReviewerVerificationController(MilanSetuDbContext db, IConfiguration configuration, ILogger<ReviewerVerificationController> logger) : ControllerBase
{
    private static readonly VerificationType[] ReviewableTypes = [VerificationType.Identity, VerificationType.Education, VerificationType.Employment];

    [HttpGet]
    public async Task<IActionResult> Queue(VerificationType? type, VerificationStatus? status, int page = 1, int pageSize = 25, CancellationToken ct = default)
    {
        if (!await IsReviewerAsync(ct)) return Forbid();
        page = Math.Clamp(page, 1, 10000);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = db.VerificationRequests.AsNoTracking().Where(x => ReviewableTypes.Contains(x.Type));
        if (type is not null) query = query.Where(x => x.Type == type.Value);
        if (status is not null) query = query.Where(x => x.Status == status.Value);
        else query = query.Where(x => x.Status == VerificationStatus.Pending);
        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(x => x.RequestedAt).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new { x.Id, x.UserId, type = x.Type.ToString(), status = x.Status.ToString(), x.RequestedAt, x.ClaimedByUserId, x.ClaimedAt, x.ReviewedAt, x.ReviewedByUserId })
            .ToListAsync(ct);
        return Ok(new { page, pageSize, total, items });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Detail(Guid id, CancellationToken ct)
    {
        if (!await IsReviewerAsync(ct)) return Forbid();
        var request = await db.VerificationRequests.AsNoTracking().Include(x => x.User).FirstOrDefaultAsync(x => x.Id == id && ReviewableTypes.Contains(x.Type), ct);
        if (request is null) return NotFound();
        return Ok(new
        {
            request.Id,
            request.UserId,
            type = request.Type.ToString(),
            status = request.Status.ToString(),
            request.RequestedAt,
            request.ClaimedByUserId,
            request.ClaimedAt,
            request.ReviewedAt,
            request.ReviewedByUserId,
            notes = request.ReviewerNotes,
            user = new { request.User.Id, request.User.Email, request.User.PhoneNumber }
        });
    }

    [HttpPost("{id:guid}/claim")]
    public async Task<IActionResult> Claim(Guid id, CancellationToken ct)
    {
        if (!await IsReviewerAsync(ct)) return Forbid();
        if (!TryGetUserId(out var reviewerId)) return Unauthorized();
        var request = await db.VerificationRequests.FirstOrDefaultAsync(x => x.Id == id && ReviewableTypes.Contains(x.Type), ct);
        if (request is null) return NotFound();
        if (request.Status != VerificationStatus.Pending) return Conflict(new { message = "Only pending verification requests can be claimed." });
        if (request.ClaimedByUserId is not null && request.ClaimedByUserId != reviewerId && request.ClaimedAt > DateTimeOffset.UtcNow.AddMinutes(-15))
            return Conflict(new { message = "This verification is currently being reviewed by another reviewer." });
        request.ClaimedByUserId = reviewerId;
        request.ClaimedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        await WriteAuditAsync(reviewerId, request, "Claim", null, ct);
        return Ok(new { request.Id, claimedByUserId = reviewerId, request.ClaimedAt });
    }

    [HttpPost("{id:guid}/approve")]
    public Task<IActionResult> Approve(Guid id, ReviewDecisionRequest input, CancellationToken ct) => Decide(id, VerificationStatus.Verified, input, ct);

    [HttpPost("{id:guid}/reject")]
    public Task<IActionResult> Reject(Guid id, ReviewDecisionRequest input, CancellationToken ct) => Decide(id, VerificationStatus.Rejected, input, ct);

    private async Task<IActionResult> Decide(Guid id, VerificationStatus target, ReviewDecisionRequest input, CancellationToken ct)
    {
        if (!await IsReviewerAsync(ct)) return Forbid();
        if (!TryGetUserId(out var reviewerId)) return Unauthorized();
        if (target == VerificationStatus.Rejected && string.IsNullOrWhiteSpace(input.Notes)) return BadRequest(new { message = "A rejection reason is required." });
        if (input.Notes?.Length > 2000) return BadRequest(new { message = "Reviewer notes cannot exceed 2000 characters." });
        var request = await db.VerificationRequests.FirstOrDefaultAsync(x => x.Id == id && ReviewableTypes.Contains(x.Type), ct);
        if (request is null) return NotFound();
        if (request.Status != VerificationStatus.Pending) return Conflict(new { message = "This verification has already been reviewed." });
        if (request.ClaimedByUserId is not null && request.ClaimedByUserId != reviewerId && request.ClaimedAt > DateTimeOffset.UtcNow.AddMinutes(-15))
            return Conflict(new { message = "This verification is currently claimed by another reviewer." });
        request.Status = target;
        request.ReviewedAt = DateTimeOffset.UtcNow;
        request.ReviewedByUserId = reviewerId;
        request.ReviewerNotes = input.Notes?.Trim();
        request.ClaimedByUserId = null;
        request.ClaimedAt = null;
        await db.SaveChangesAsync(ct);
        await WriteAuditAsync(reviewerId, request, target == VerificationStatus.Verified ? "Approve" : "Reject", request.ReviewerNotes, ct);
        return Ok(new { request.Id, status = request.Status.ToString(), request.ReviewedAt, request.ReviewedByUserId, notes = request.ReviewerNotes });
    }

    private async Task WriteAuditAsync(Guid reviewerId, VerificationRequest request, string action, string? notes, CancellationToken ct)
    {
        db.Set<ReviewerAuditEvent>().Add(new ReviewerAuditEvent { ReviewerUserId = reviewerId, VerificationRequestId = request.Id, VerificationType = request.Type, Action = action, Notes = notes });
        try { await db.SaveChangesAsync(ct); }
        catch (Exception ex) { logger.LogError(ex, "Unable to persist reviewer audit event for verification {VerificationRequestId}", request.Id); }
    }

    private async Task<bool> IsReviewerAsync(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return false;
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId && x.IsActive, ct);
        if (user is null) return false;
        var reviewers = configuration.GetSection("Authorization:ReviewerEmails").Get<string[]>() ?? [];
        var admins = configuration.GetSection("Authorization:AdminEmails").Get<string[]>() ?? [];
        return reviewers.Concat(admins).Any(x => string.Equals(x.Trim(), user.Email, StringComparison.OrdinalIgnoreCase));
    }

    private bool TryGetUserId(out Guid userId) => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out userId);
}

public sealed record ReviewDecisionRequest(string? Notes);
