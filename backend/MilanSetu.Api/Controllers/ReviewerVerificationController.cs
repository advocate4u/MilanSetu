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
    private bool IsReviewer()
    {
        var email = User.FindFirstValue("email");
        var configured = configuration.GetSection("Verification:ReviewerEmails").Get<string[]>() ?? [];
        return !string.IsNullOrWhiteSpace(email) && configured.Any(x => string.Equals(x.Trim(), email, StringComparison.OrdinalIgnoreCase));
    }

    [HttpGet]
    public async Task<IActionResult> Queue(VerificationType? type, VerificationStatus? status, int page = 1, int pageSize = 25, CancellationToken cancellationToken = default)
    {
        if (!IsReviewer()) return Forbid();
        page = Math.Clamp(page, 1, 10000); pageSize = Math.Clamp(pageSize, 1, 100);
        var query = db.VerificationRequests.AsNoTracking().Where(x => x.Type == VerificationType.Identity || x.Type == VerificationType.Education || x.Type == VerificationType.Employment);
        if (type.HasValue) query = query.Where(x => x.Type == type.Value);
        if (status.HasValue) query = query.Where(x => x.Status == status.Value); else query = query.Where(x => x.Status == VerificationStatus.Pending);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(x => x.RequestedAt).Skip((page - 1) * pageSize).Take(pageSize).Select(x => new { x.Id, x.UserId, x.Type, x.Status, x.RequestedAt, x.ReviewedAt, x.ReviewerNotes }).ToListAsync(cancellationToken);
        return Ok(new { page, pageSize, total, items });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        if (!IsReviewer()) return Forbid();
        var item = await db.VerificationRequests.AsNoTracking().Include(x => x.User).Include(x => x.User.Profile).Include(x => x.User.Profile!.Education).Include(x => x.User.Profile!.Employment).SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null || item.Type is VerificationType.Mobile or VerificationType.Email) return NotFound();
        return Ok(new { item.Id, item.UserId, item.Type, item.Status, item.RequestedAt, item.ReviewedAt, item.ReviewerNotes, user = new { item.User.Email, item.User.PhoneNumber }, profile = item.User.Profile is null ? null : new { item.User.Profile.DisplayName, education = item.User.Profile.Education, employment = item.User.Profile.Employment } });
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, ReviewDecisionRequest request, CancellationToken cancellationToken)
        => await Decide(id, true, request, cancellationToken);

    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, ReviewDecisionRequest request, CancellationToken cancellationToken)
        => await Decide(id, false, request, cancellationToken);

    private async Task<IActionResult> Decide(Guid id, bool approve, ReviewDecisionRequest request, CancellationToken cancellationToken)
    {
        if (!IsReviewer()) return Forbid();
        var reviewerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);
        var item = await db.VerificationRequests.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null || item.Type is VerificationType.Mobile or VerificationType.Email) return NotFound();
        if (item.Status != VerificationStatus.Pending) return Conflict(new { message = "Only pending verification requests can be reviewed." });
        var notes = request.Notes?.Trim();
        if (!approve && string.IsNullOrWhiteSpace(notes)) return BadRequest(new { message = "A rejection reason is required." });
        if (notes?.Length > 2000) return BadRequest(new { message = "Review notes cannot exceed 2000 characters." });
        item.Status = approve ? VerificationStatus.Verified : VerificationStatus.Rejected;
        item.ReviewedAt = DateTimeOffset.UtcNow;
        item.VerifiedAt = approve ? item.ReviewedAt : null;
        item.ReviewerNotes = notes;
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Verification {VerificationId} {Decision} by reviewer {ReviewerId} for user {UserId}", id, approve ? "approved" : "rejected", reviewerId, item.UserId);
        return Ok(new { item.Id, item.Type, item.Status, item.ReviewedAt });
    }
}

public sealed record ReviewDecisionRequest(string? Notes);
