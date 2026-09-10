using MilanSetu.Api.Data;
using MilanSetu.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MilanSetu.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/reports")]
public sealed class ReportsController(MilanSetuDbContext db) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateReportRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var reporterId)) return Unauthorized();
        if (reporterId == request.ReportedUserId) return BadRequest(new { message = "You cannot report yourself." });
        if (!await db.Users.AnyAsync(x => x.Id == request.ReportedUserId, ct)) return NotFound();
        if (!Enum.IsDefined(request.Reason)) return BadRequest(new { message = "Invalid report reason." });

        var details = request.Details?.Trim();
        if (details?.Length > 2000) return BadRequest(new { message = "Report details are too long." });

        var report = new UserReport
        {
            Id = Guid.NewGuid(),
            ReporterUserId = reporterId,
            ReportedUserId = request.ReportedUserId,
            Reason = request.Reason,
            Details = details
        };

        db.UserReports.Add(report);
        db.Notifications.Add(new Notification
        {
            Id = Guid.NewGuid(),
            UserId = reporterId,
            Type = NotificationType.SafetyNotice,
            Title = "Report submitted",
            Body = "Your safety report has been submitted for review.",
            RelatedUserId = request.ReportedUserId,
            RelatedEntityId = report.Id
        });

        await db.SaveChangesAsync(ct);
        return Ok(new { report.Id, status = report.Status.ToString() });
    }

    private bool TryGetUserId(out Guid userId) =>
        Guid.TryParse(User.FindFirst("sub")?.Value, out userId);
}

public sealed record CreateReportRequest(Guid ReportedUserId, ReportReason Reason, string? Details);
