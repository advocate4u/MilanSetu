using MilanSetu.Api.Data;
using MilanSetu.Api.Data.Repositories;
using MilanSetu.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MilanSetu.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/block")]
public sealed class BlockController(IUnitOfWork unitOfWork, MilanSetuDbContext db) : ControllerBase
{
    private IRepository<User> Users => unitOfWork.Repository<User>();
    private IRepository<Block> Blocks => unitOfWork.Repository<Block>();

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        if (!TryGetUserId(out var blockerId)) return Unauthorized();
        var items = await db.Blocks.AsNoTracking()
            .Where(x => x.BlockerUserId == blockerId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                userId = x.BlockedUserId,
                displayName = db.Profiles.Where(p => p.UserId == x.BlockedUserId)
                    .Select(p => p.DisplayName).FirstOrDefault() ?? "Member",
                blockedAt = x.CreatedAt
            })
            .ToListAsync(ct);
        return Ok(items);
    }

    [HttpPost("{userId:guid}")]
    public async Task<IActionResult> Block(Guid userId, CancellationToken ct)
    {
        if (!TryGetUserId(out var blockerId)) return Unauthorized();
        if (blockerId == userId) return BadRequest(new { message = "You cannot block yourself." });
        if (!await Users.AnyAsync(x => x.Id == userId, ct)) return NotFound();
        if (!await Blocks.AnyAsync(x => x.BlockerUserId == blockerId && x.BlockedUserId == userId, ct))
            Blocks.Add(new Block { Id = Guid.NewGuid(), BlockerUserId = blockerId, BlockedUserId = userId });
        await unitOfWork.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{userId:guid}")]
    public async Task<IActionResult> Unblock(Guid userId, CancellationToken ct)
    {
        if (!TryGetUserId(out var blockerId)) return Unauthorized();
        var block = await Blocks.SingleOrDefaultAsync(x => x.BlockerUserId == blockerId && x.BlockedUserId == userId, ct);
        if (block is not null) { Blocks.Remove(block); await unitOfWork.SaveChangesAsync(ct); }
        return NoContent();
    }

    private bool TryGetUserId(out Guid userId) => Guid.TryParse(User.FindFirst("sub")?.Value, out userId);
}
