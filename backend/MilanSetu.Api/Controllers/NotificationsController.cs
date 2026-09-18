using MilanSetu.Api.Data.Repositories;
using MilanSetu.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MilanSetu.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/notifications")]
public sealed class NotificationsController(IUnitOfWork unitOfWork) : ControllerBase
{
    private IRepository<Notification> Notifications => unitOfWork.Repository<Notification>();

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] bool unreadOnly = false, [FromQuery] int limit = 50, CancellationToken ct = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        limit = Math.Clamp(limit, 1, 100);

        var query = Notifications.Query().Where(x => x.UserId == userId);
        if (unreadOnly) query = query.Where(x => x.ReadAt == null);

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Take(limit)
            .Select(x => new
            {
                x.Id,
                type = x.Type.ToString(),
                x.Title,
                x.Body,
                x.RelatedUserId,
                x.RelatedEntityId,
                x.CreatedAt,
                x.ReadAt
            })
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpGet("summary")]
    public async Task<IActionResult> Summary(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var query = Notifications.Query().Where(x => x.UserId == userId);
        var unreadCount = await query.CountAsync(x => x.ReadAt == null, ct);
        var latest = await query.OrderByDescending(x => x.CreatedAt).Select(x => (DateTimeOffset?)x.CreatedAt).FirstOrDefaultAsync(ct);
        var unreadByType = await query.Where(x => x.ReadAt == null).GroupBy(x => x.Type).Select(x => new { type = x.Key.ToString(), count = x.Count() }).ToListAsync(ct);
        return Ok(new { unreadCount, latestCreatedAt = latest, unreadByType });
    }

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var notification = await Notifications.Query(false).SingleOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct);
        if (notification is null) return NotFound();

        notification.ReadAt ??= DateTimeOffset.UtcNow;
        await unitOfWork.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var notifications = await Notifications.Query(false)
            .Where(x => x.UserId == userId && x.ReadAt == null)
            .ToListAsync(ct);

        var now = DateTimeOffset.UtcNow;
        foreach (var notification in notifications) notification.ReadAt = now;
        await unitOfWork.SaveChangesAsync(ct);
        return NoContent();
    }

    private bool TryGetUserId(out Guid userId) => Guid.TryParse(User.FindFirst("sub")?.Value, out userId);
}
