using MilanSetu.Api.Data;
using MilanSetu.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MilanSetu.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/interests")]
public sealed class InterestsController(MilanSetuDbContext db) : ControllerBase
{
    [HttpPost("{profileId:guid}")]
    public async Task<IActionResult> Send(Guid profileId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var target = await db.Profiles.AsNoTracking().SingleOrDefaultAsync(x => x.Id == profileId, ct);
        if (target is null || target.Visibility == ProfileVisibility.Hidden) return NotFound();
        if (target.UserId == userId) return BadRequest(new { message = "You cannot express interest in your own profile." });
        if (await IsBlocked(userId, target.UserId, ct)) return Conflict(new { message = "This connection is unavailable." });

        var existing = await db.Interests.SingleOrDefaultAsync(x => x.SenderUserId == userId && x.ReceiverUserId == target.UserId, ct);
        if (existing is not null)
        {
            if (existing.Status == InterestStatus.Declined || existing.Status == InterestStatus.Cancelled)
            {
                existing.Status = InterestStatus.Pending;
                existing.RespondedAt = null;
                AddNotification(target.UserId, NotificationType.InterestReceived, "New interest", "Someone expressed interest in your profile.", userId, existing.Id);
                await db.SaveChangesAsync(ct);
                return Ok(new { id = existing.Id, status = existing.Status.ToString() });
            }
            return Conflict(new { message = "Interest already exists.", status = existing.Status.ToString() });
        }

        var interest = new Interest { Id = Guid.NewGuid(), SenderUserId = userId, ReceiverUserId = target.UserId };
        db.Interests.Add(interest);
        AddNotification(target.UserId, NotificationType.InterestReceived, "New interest", "Someone expressed interest in your profile.", userId, interest.Id);
        await db.SaveChangesAsync(ct);
        return Ok(new { id = interest.Id, status = interest.Status.ToString() });
    }

    [HttpGet("incoming")]
    public async Task<IActionResult> Incoming(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var items = await db.Interests.AsNoTracking().Where(x => x.ReceiverUserId == userId && x.Status == InterestStatus.Pending)
            .OrderByDescending(x => x.CreatedAt).Select(x => new { x.Id, x.SenderUserId, x.CreatedAt }).ToListAsync(ct);
        return Ok(items);
    }

    [HttpGet("outgoing")]
    public async Task<IActionResult> Outgoing(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var items = await db.Interests.AsNoTracking().Where(x => x.SenderUserId == userId)
            .OrderByDescending(x => x.CreatedAt).Select(x => new { x.Id, x.ReceiverUserId, status = x.Status.ToString(), x.CreatedAt, x.RespondedAt }).ToListAsync(ct);
        return Ok(items);
    }

    [HttpPost("{id:guid}/accept")]
    public async Task<IActionResult> Accept(Guid id, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var interest = await db.Interests.SingleOrDefaultAsync(x => x.Id == id && x.ReceiverUserId == userId, ct);
        if (interest is null) return NotFound();
        if (interest.Status != InterestStatus.Pending) return Conflict(new { message = "Interest is no longer pending." });
        if (await IsBlocked(userId, interest.SenderUserId, ct)) return Conflict(new { message = "This connection is unavailable." });

        interest.Status = InterestStatus.Accepted;
        interest.RespondedAt = DateTimeOffset.UtcNow;
        var a = interest.SenderUserId.CompareTo(interest.ReceiverUserId) < 0 ? interest.SenderUserId : interest.ReceiverUserId;
        var b = interest.SenderUserId.CompareTo(interest.ReceiverUserId) < 0 ? interest.ReceiverUserId : interest.SenderUserId;
        if (!await db.Connections.AnyAsync(x => x.UserAId == a && x.UserBId == b, ct))
            db.Connections.Add(new Connection { Id = Guid.NewGuid(), UserAId = a, UserBId = b });
        AddNotification(interest.SenderUserId, NotificationType.InterestAccepted, "Interest accepted", "Your interest was accepted. You can now connect and chat.", userId, interest.Id);
        await db.SaveChangesAsync(ct);
        return Ok(new { status = "Accepted", connectionCreated = true });
    }

    [HttpPost("{id:guid}/decline")]
    public async Task<IActionResult> Decline(Guid id, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var interest = await db.Interests.SingleOrDefaultAsync(x => x.Id == id && x.ReceiverUserId == userId, ct);
        if (interest is null) return NotFound();
        if (interest.Status != InterestStatus.Pending) return Conflict(new { message = "Interest is no longer pending." });
        interest.Status = InterestStatus.Declined;
        interest.RespondedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(new { status = "Declined" });
    }

    private async Task<bool> IsBlocked(Guid userId, Guid otherUserId, CancellationToken ct) =>
        await db.Blocks.AnyAsync(x => (x.BlockerUserId == userId && x.BlockedUserId == otherUserId) || (x.BlockerUserId == otherUserId && x.BlockedUserId == userId), ct);

    private void AddNotification(Guid userId, NotificationType type, string title, string body, Guid relatedUserId, Guid relatedEntityId) =>
        db.Notifications.Add(new Notification { Id = Guid.NewGuid(), UserId = userId, Type = type, Title = title, Body = body, RelatedUserId = relatedUserId, RelatedEntityId = relatedEntityId });

    private bool TryGetUserId(out Guid userId) => Guid.TryParse(User.FindFirst("sub")?.Value, out userId);
}
