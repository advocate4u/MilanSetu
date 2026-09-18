using MilanSetu.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MilanSetu.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/admin/metrics")]
public sealed class AdminMetricsController(RequestMetricsService metrics) : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        if (!User.Claims.Any(x => x.Type == "role" && x.Value.Equals("Admin", StringComparison.OrdinalIgnoreCase)))
            return Forbid();
        return Ok(metrics.Snapshot());
    }
}