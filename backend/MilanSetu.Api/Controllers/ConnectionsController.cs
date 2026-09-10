using MilanSetu.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MilanSetu.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/connections")]
public sealed class ConnectionsController(MilanSetuDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var blockedUserIds = db.Blocks
            .Where(x => x.BlockerUserId == userId || x.BlockedUserId == userId)
            .Select(x => x.BlockerUserId == userId ? x.BlockedUserId : x.BlockerUserId);

        var items = await db.Connections.AsNoTracking()
            .Where(x => (x.UserAId == userId || x.UserBId == userId) &&
                        !blockedUserIds.Contains(x.UserAId == userId ? x.UserBId : x.UserAId))
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.Id,
                otherUserId = x.UserAId == userId ? x.UserBId : x.UserAId,
                x.CreatedAt
            })
            .ToListAsync(ct);

        return Ok(items);
    }

    private bool TryGetUserId(out Guid userId) =>
        Guid.TryParse(User.FindFirst("sub")?.Value, out userId);
}
