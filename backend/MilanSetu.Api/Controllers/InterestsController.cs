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
    public async Task<IActionResult> Send(Guid profileId,CancellationToken ct){var me=GetUserId();var target=await db.Profiles.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==profileId,ct);if(target is null||target.UserId==me||target.Visibility==ProfileVisibility.Hidden)return NotFound(new{message="Profile is not available."});if(await db.Blocks.AnyAsync(x=>(x.BlockerUserId==me&&x.BlockedUserId==target.UserId)||(x.BlockerUserId==target.UserId&&x.BlockedUserId==me),ct))return Conflict(new{message="This connection is blocked."});if(await db.Interests.AnyAsync(x=>x.SenderUserId==me&&x.ReceiverUserId==target.UserId,ct))return Conflict(new{message="Interest already exists."});db.Interests.Add(new Interest{Id=Guid.NewGuid(),SenderUserId=me,ReceiverUserId=target.UserId});await db.SaveChangesAsync(ct);return Ok(new{message="Interest sent.",status=InterestStatus.Pending.ToString()});}
    [HttpGet("incoming")]
    public async Task<IActionResult> Incoming(CancellationToken ct)=>Ok(await db.Interests.AsNoTracking().Where(x=>x.ReceiverUserId==GetUserId()).OrderByDescending(x=>x.CreatedAt).Select(x=>new{x.Id,x.Status,x.CreatedAt,senderProfileId=db.Profiles.Where(p=>p.UserId==x.SenderUserId).Select(p=>p.Id).FirstOrDefault(),senderName=db.Profiles.Where(p=>p.UserId==x.SenderUserId).Select(p=>p.DisplayName).FirstOrDefault()}).ToListAsync(ct));
    [HttpGet("outgoing")]
    public async Task<IActionResult> Outgoing(CancellationToken ct)=>Ok(await db.Interests.AsNoTracking().Where(x=>x.SenderUserId==GetUserId()).OrderByDescending(x=>x.CreatedAt).Select(x=>new{x.Id,x.Status,x.CreatedAt,receiverProfileId=db.Profiles.Where(p=>p.UserId==x.ReceiverUserId).Select(p=>p.Id).FirstOrDefault(),receiverName=db.Profiles.Where(p=>p.UserId==x.ReceiverUserId).Select(p=>p.DisplayName).FirstOrDefault()}).ToListAsync(ct));
    [HttpPost("{id:guid}/accept")]
    public async Task<IActionResult> Accept(Guid id,CancellationToken ct){var me=GetUserId();var x=await db.Interests.SingleOrDefaultAsync(i=>i.Id==id&&i.ReceiverUserId==me,ct);if(x is null)return NotFound();if(x.Status!=InterestStatus.Pending)return Conflict(new{message="Interest is no longer pending."});if(await db.Blocks.AnyAsync(b=>(b.BlockerUserId==me&&b.BlockedUserId==x.SenderUserId)||(b.BlockerUserId==x.SenderUserId&&b.BlockedUserId==me),ct))return Conflict(new{message="This connection is blocked."});x.Status=InterestStatus.Accepted;x.RespondedAt=DateTimeOffset.UtcNow;var a=x.SenderUserId.CompareTo(x.ReceiverUserId)<0?x.SenderUserId:x.ReceiverUserId;var b=a==x.SenderUserId?x.ReceiverUserId:x.SenderUserId;if(!await db.Connections.AnyAsync(c=>c.UserAId==a&&c.UserBId==b,ct))db.Connections.Add(new Connection{Id=Guid.NewGuid(),UserAId=a,UserBId=b});await db.SaveChangesAsync(ct);return Ok(new{message="Interest accepted. You are now connected."});}
    [HttpPost("{id:guid}/decline")]
    public async Task<IActionResult> Decline(Guid id,CancellationToken ct){var x=await db.Interests.SingleOrDefaultAsync(i=>i.Id==id&&i.ReceiverUserId==GetUserId(),ct);if(x is null)return NotFound();if(x.Status!=InterestStatus.Pending)return Conflict();x.Status=InterestStatus.Declined;x.RespondedAt=DateTimeOffset.UtcNow;await db.SaveChangesAsync(ct);return Ok(new{message="Interest declined."});}
    private Guid GetUserId()=>Guid.TryParse(User.FindFirst("sub")?.Value,out var id)?id:throw new UnauthorizedAccessException();
}
