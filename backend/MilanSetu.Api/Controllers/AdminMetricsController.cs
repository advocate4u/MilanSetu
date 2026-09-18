using MilanSetu.Api.Data;
using MilanSetu.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MilanSetu.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/admin/metrics")]
public sealed class AdminMetricsController(RequestMetricsService metrics, MilanSetuDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var userId) ||
            !await db.UserRoleAssignments.AsNoTracking().AnyAsync(x => x.UserId == userId && x.Role == Domain.UserRole.Admin, ct))
            return Forbid();
        return Ok(metrics.Snapshot());
    }
}
