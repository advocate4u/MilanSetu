using MilanSetu.Api.Data;
using MilanSetu.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MilanSetu.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/block")]
public sealed class BlockController(MilanSetuDbContext db) : ControllerBase
{
    [HttpPost("{userId:guid}")]
    public async Task<IActionResult> Create(Guid userId,CancellationToken ct){var me=GetUserId();if(me==userId)return BadRequest(new{message="You cannot block yourself."});if(!await db.Users.AnyAsync(x=>x.Id==userId,ct))return NotFound();if(!await db.Blocks.AnyAsync(x=>x.BlockerUserId==me&&x.BlockedUserId==userId,ct)){db.Blocks.Add(new Block{Id=Guid.NewGuid(),BlockerUserId=me,BlockedUserId=userId});await db.SaveChangesAsync(ct);}return Ok(new{message="User blocked."});}
    [HttpDelete("{userId:guid}")]
    public async Task<IActionResult> Remove(Guid userId,CancellationToken ct){var x=await db.Blocks.SingleOrDefaultAsync(b=>b.BlockerUserId==GetUserId()&&b.BlockedUserId==userId,ct);if(x is null)return NotFound();db.Blocks.Remove(x);await db.SaveChangesAsync(ct);return Ok(new{message="User unblocked."});}
    private Guid GetUserId()=>Guid.TryParse(User.FindFirst("sub")?.Value,out var id)?id:throw new UnauthorizedAccessException();
}
