using MilanSetu.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MilanSetu.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/security")]
public sealed class SecurityController(SecurityAuditService securityAudit) : ControllerBase
{
    [HttpGet("sessions")]
    public async Task<IActionResult> Sessions(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var currentSessionId = GetSessionId();
        var sessions = await securityAudit.GetSessionsAsync(userId, cancellationToken);

        return Ok(sessions.Select(x => new
        {
            sessionId = x.Id,
            isCurrent = x.Id == currentSessionId,
            loginAt = x.LoginAt,
            lastSeenAt = x.LastSeenAt,
            logoutAt = x.LogoutAt,
            revokedAt = x.RevokedAt,
            status = x.LoginStatus,
            provider = x.LoginProvider,
            ipAddress = x.IPAddress,
            location = new { country = x.Country, state = x.State, city = x.City },
            device = new
            {
                type = x.DeviceType,
                model = x.DeviceModel,
                name = x.DeviceName,
                deviceId = x.DeviceId,
                os = x.OS,
                osVersion = x.OSVersion,
                appVersion = x.AppVersion,
                browser = x.Browser,
                browserVersion = x.BrowserVersion
            }
        }));
    }

    [HttpDelete("sessions/{sessionId:guid}")]
    public async Task<IActionResult> RevokeSession(Guid sessionId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var revoked = await securityAudit.RevokeSessionAsync(userId, sessionId, cancellationToken);
        if (!revoked) return NotFound();

        var context = securityAudit.Capture();
        await securityAudit.RecordAsync(userId, sessionId, "SESSION_REVOKED", true, "session", context, cancellationToken: cancellationToken);
        return NoContent();
    }

    [HttpGet("activity")]
    public async Task<IActionResult> Activity([FromQuery] int take = 50, CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        take = Math.Clamp(take, 1, 100);
        var logs = await securityAudit.GetActivityAsync(userId, take, cancellationToken);
        return Ok(logs.Select(x => new
        {
            id = x.Id,
            eventType = x.EventType,
            timestamp = x.Timestamp,
            success = x.Success,
            failureReason = x.FailureReason,
            ipAddress = x.IPAddress,
            provider = x.Provider,
            sessionId = x.SessionId,
            deviceId = x.DeviceId,
            metadata = x.Metadata
        }));
    }

    private bool TryGetUserId(out Guid userId)
        => Guid.TryParse(User.FindFirst("sub")?.Value, out userId);

    private Guid? GetSessionId()
        => Guid.TryParse(User.FindFirst("sid")?.Value, out var id) ? id : null;
}
