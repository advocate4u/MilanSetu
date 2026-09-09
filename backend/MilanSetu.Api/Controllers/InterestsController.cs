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
        var me=GetUserId(); var target=await db.Profiles.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==profileId,ct);
        if(target is null || target.UserId==me || target.Visibility==ProfileVisibility.Hidden)return NotFound(new{message="Profile is not available."});
        if(await db.Blocks.AnyAsync(x=>(x.BlockerUserId==me&&x.BlockedUserId==target.UserId)||(x.BlockerUserId==target.UserId&&x.BlockedUserId==me),ct))return Conflict(new{message="This connection is blocked."});
        var existing=await db.Interests.SingleOrDefaultAsync(x=>x.SenderUserId==me&&x.ReceiverUserId==target.UserId,ct);
        if(existing is not null)return Conflict(new{message="Interest already exists.",status=existing.Status.ToString()});
        db.Interests.Add(new Interest{Id=Guid.NewGuid(),SenderUserId=me,ReceiverUserId=target.UserId}); await db.SaveChangesAsync(ct);
        return Ok(new{message="Interest sent.",status=InterestStatus.Pending.ToString()});
    }

    [HttpGet("incoming")]
    public async Task<IActionResult> Incoming(CancellationToken ct) => Ok(await db.Interests.AsNoTracking().Where(x=>x.ReceiverUserId==GetUserId()).OrderByDescending(x=>x.CreatedAt).Select(x=>new{x.Id,x.Status,x.CreatedAt,senderProfileId=db.Profiles.Where(p=>p.UserId==x.SenderUserId).Select(p=>p.Id).FirstOrDefault(),senderName=db.Profiles.Where(p=>p.UserId==x.SenderUserId).Select(p=>p.DisplayName).FirstOrDefault()}).ToListAsync(ct));

    [HttpGet("outgoing")]
    public async Task<IActionResult> Outgoing(CancellationToken ct) => Ok(await db.Interests.AsNoTracking().Where(x=>x.SenderUserId==GetUserId()).OrderByDescending(x=>x.CreatedAt).Select(x=>new{x.Id,x.Status,x.CreatedAt,receiverProfileId=db.Profiles.Where(p=>p.UserId==x.ReceiverUserId).Select(p=>p.Id).FirstOrDefault(),receiverName=db.Profiles.Where(p=>p.UserId==x.ReceiverUserId).Select(p=>p.DisplayName).FirstOrDefault()}).ToListAsync(ct));

    [HttpPost("{id:guid}/accept")]
    public async Task<IActionResult> Accept(Guid id,CancellationToken ct)
    {
        var me=GetUserId(); var interest=await db.Interests.SingleOrDefaultAsync(x=>x.Id==id&&x.ReceiverUserId==me,ct); if(interest is null)return NotFound();
        if(interest.Status!=InterestStatus.Pending)return Conflict(new{message="Interest is no longer pending."});
        if(await db.Blocks.AnyAsync(x=>(x.BlockerUserId==me&&x.BlockedUserId==interest.SenderUserId)||(x.BlockerUserId==interest.SenderUserId&&x.BlockedUserId==me),ct))return Conflict(new{message="This connection is blocked."});
        interest.Status=InterestStatus.Accepted; interest.RespondedAt=DateTimeOffset.UtcNow;
        var a=interest.SenderUserId.CompareTo(interest.ReceiverUserId)<0?interest.SenderUserId:interest.ReceiverUserId; var b=a==interest.SenderUserId?interest.ReceiverUserId:interest.SenderUserId;
        if(!await db.Connections.AnyAsync(x=>x.UserAId==a&&x.UserBId==b,ct))db.Connections.Add(new Connection{Id=Guid.NewGuid(),UserAId=a,UserBId=b});
        await db.SaveChangesAsync(ct); return Ok(new{message="Interest accepted. You are now connected."});
    }

    [HttpPost("{id:guid}/decline")]
    public async Task<IActionResult> Decline(Guid id,CancellationToken ct){var x=await db.Interests.SingleOrDefaultAsync(i=>i.Id==id&&i.ReceiverUserId==GetUserId(),ct);if(x is null)return NotFound();if(x.Status!=InterestStatus.Pending)return Conflict();x.Status=InterestStatus.Declined;x.RespondedAt=DateTimeOffset.UtcNow;await db.SaveChangesAsync(ct);return Ok(new{message="Interest declined."});}

    [HttpPost("../block/{userId:guid}")]
    public async Task<IActionResult> Block(Guid userId,CancellationToken ct){var me=GetUserId();if(me==userId)return BadRequest(new{message="You cannot block yourself."});if(!await db.Users.AnyAsync(x=>x.Id==userId,ct))return NotFound();if(!await db.Blocks.AnyAsync(x=>x.BlockerUserId==me&&x.BlockedUserId==userId,ct)){db.Blocks.Add(new Block{Id=Guid.NewGuid(),BlockerUserId=me,BlockedUserId=userId});await db.SaveChangesAsync(ct);}return Ok(new{message="User blocked."});}
    private Guid GetUserId()=>Guid.TryParse(User.FindFirst("sub")?.Value,out var id)?id:throw new UnauthorizedAccessException();
}
