using MilanSetu.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MilanSetu.Api.Domain;

namespace MilanSetu.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/block")]
public sealed class BlockController(MilanSetuDbContext db) : ControllerBase
{
    [HttpPost("{userId:guid}")]
    public async Task<IActionResult> Block(Guid userId, CancellationToken ct)
    {
        if (!TryGetUserId(out var blockerId)) return Unauthorized();
        if (blockerId == userId) return BadRequest(new { message = "You cannot block yourself." });
        if (!await db.Users.AnyAsync(x => x.Id == userId, ct)) return NotFound();
        if (!await db.Blocks.AnyAsync(x => x.BlockerUserId == blockerId && x.BlockedUserId == userId, ct))
            db.Blocks.Add(new Block { Id = Guid.NewGuid(), BlockerUserId = blockerId, BlockedUserId = userId });
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{userId:guid}")]
    public async Task<IActionResult> Unblock(Guid userId, CancellationToken ct)
    {
        if (!TryGetUserId(out var blockerId)) return Unauthorized();
        var block = await db.Blocks.SingleOrDefaultAsync(x => x.BlockerUserId == blockerId && x.BlockedUserId == userId, ct);
        if (block is not null) { db.Blocks.Remove(block); await db.SaveChangesAsync(ct); }
        return NoContent();
    }

    private bool TryGetUserId(out Guid userId) => Guid.TryParse(User.FindFirst("sub")?.Value, out userId);
}
