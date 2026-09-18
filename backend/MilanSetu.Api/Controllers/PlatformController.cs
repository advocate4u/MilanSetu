using MilanSetu.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MilanSetu.Api.Controllers;

[ApiController]
[Route("api/platform")]
public sealed class PlatformController(MilanSetuDbContext db) : ControllerBase
{
    [HttpGet("settings")]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var s = await db.PlatformSettings.AsNoTracking().SingleOrDefaultAsync(x => x.Id == 1, ct);
        return Ok(new { messagingEnabled = s?.MessagingEnabled ?? true, maxProfilePhotos = Math.Clamp(s?.MaxProfilePhotos ?? 6, 1, 20) });
    }
}