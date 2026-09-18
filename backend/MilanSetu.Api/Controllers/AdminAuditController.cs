using MilanSetu.Api.Data;
using MilanSetu.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace MilanSetu.Api.Controllers;
[ApiController][Authorize][Route("api/admin/audit")]
public sealed class AdminAuditController(MilanSetuDbContext db):ControllerBase{
[HttpGet] public async Task<IActionResult> List(string? action,string? resourceType,Guid? actorUserId,Guid? resourceId,DateTimeOffset? from,DateTimeOffset? to,int page=1,int pageSize=50,CancellationToken ct=default){if(!await IsAdmin(ct))return Forbid();page=Math.Clamp(page,1,10000);pageSize=Math.Clamp(pageSize,1,100);action=action?.Trim();resourceType=resourceType?.Trim();var q=db.AuditLogs.AsNoTracking();if(!string.IsNullOrWhiteSpace(action))q=q.Where(x=>x.Action.Contains(action));if(!string.IsNullOrWhiteSpace(resourceType))q=q.Where(x=>x.ResourceType==resourceType);if(actorUserId.HasValue)q=q.Where(x=>x.ActorUserId==actorUserId);if(resourceId.HasValue)q=q.Where(x=>x.ResourceId==resourceId);if(from.HasValue)q=q.Where(x=>x.CreatedAt>=from.Value);if(to.HasValue)q=q.Where(x=>x.CreatedAt<=to.Value);var total=await q.CountAsync(ct);var items=await q.OrderByDescending(x=>x.CreatedAt).ThenByDescending(x=>x.Id).Skip((page-1)*pageSize).Take(pageSize).Select(x=>new{x.Id,x.ActorUserId,actorEmail=x.ActorUser==null?null:x.ActorUser.Email,x.Action,x.ResourceType,x.ResourceId,x.Metadata,x.CreatedAt}).ToListAsync(ct);return Ok(new{page,pageSize,total,items});}
private async Task<bool> IsAdmin(CancellationToken ct){if(!Guid.TryParse(User.FindFirst("sub")?.Value,out var id))return false;return await db.UserRoleAssignments.AsNoTracking().AnyAsync(x=>x.UserId==id&&x.Role==UserRole.Admin,ct);}}
