using MilanSetu.Api.Data;
using MilanSetu.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace MilanSetu.Api.Controllers;
[ApiController][Authorize][Route("api/admin/dashboard")]
public sealed class AdminDashboardController(MilanSetuDbContext db):ControllerBase{
[HttpGet] public async Task<IActionResult> Get(CancellationToken ct){if(!await IsAdmin(ct))return Forbid();var now=DateTimeOffset.UtcNow;var dayAgo=now.AddDays(-1);var weekAgo=now.AddDays(-7);var users=await db.Users.AsNoTracking().CountAsync(ct);var activeUsers=await db.Users.AsNoTracking().CountAsync(x=>x.IsActive,ct);var newUsers24h=await db.Users.AsNoTracking().CountAsync(x=>x.CreatedAt>=dayAgo,ct);var newUsers7d=await db.Users.AsNoTracking().CountAsync(x=>x.CreatedAt>=weekAgo,ct);var pendingVerifications=await db.VerificationRequests.AsNoTracking().CountAsync(x=>x.Status==VerificationStatus.Pending,ct);var openReports=await db.UserReports.AsNoTracking().CountAsync(x=>x.Status==ReportStatus.Open||x.Status==ReportStatus.Reviewing,ct);var openModeration=await db.ModerationCases.AsNoTracking().CountAsync(x=>x.Status==ModerationStatus.Open||x.Status==ModerationStatus.Reviewing,ct);var messages24h=await db.Messages.AsNoTracking().CountAsync(x=>x.CreatedAt>=dayAgo,ct);var connections=await db.Connections.AsNoTracking().CountAsync(ct);return Ok(new{generatedAt=now,users,activeUsers,inactiveUsers=users-activeUsers,newUsers24h,newUsers7d,pendingVerifications,openReports,openModeration,messages24h,connections});}
private async Task<bool> IsAdmin(CancellationToken ct){if(!Guid.TryParse(User.FindFirst("sub")?.Value,out var id))return false;return await db.UserRoleAssignments.AsNoTracking().AnyAsync(x=>x.UserId==id&&x.Role==UserRole.Admin,ct);}}
