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

        var duplicate = await db.UserReports.AnyAsync(x =>
            x.ReporterUserId == reporterId && x.ReportedUserId == request.ReportedUserId &&
            x.Status != ReportStatus.Dismissed && x.CreatedAt > DateTimeOffset.UtcNow.AddHours(-24), ct);
        if (duplicate) return Conflict(new { message = "A recent report for this profile is already under review." });

        var report = new UserReport
        {
            Id = Guid.NewGuid(),
            ReporterUserId = reporterId,
            ReportedUserId = request.ReportedUserId,
            Reason = request.Reason,
            Details = details
        };
        db.UserReports.Add(report);

        db.ModerationCases.Add(new ModerationCase
        {
            Id = Guid.NewGuid(),
            ReportId = report.Id,
            TargetUserId = report.ReportedUserId,
            Severity = GetInitialSeverity(report.Reason),
            Status = ModerationStatus.Open,
            Action = ModerationAction.None
        });

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

    [HttpGet("mine")]
    public async Task<IActionResult> Mine(CancellationToken ct)
    {
        if (!TryGetUserId(out var reporterId)) return Unauthorized();

        var reports = await db.UserReports.AsNoTracking()
            .Where(x => x.ReporterUserId == reporterId)
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Select(x => new { x.Id, x.ReportedUserId, reason = x.Reason.ToString(), status = x.Status.ToString(), x.CreatedAt, x.ResolvedAt })
            .Take(100)
            .ToListAsync(ct);

        return Ok(reports);
    }

    private static ModerationSeverity GetInitialSeverity(ReportReason reason) => reason switch
    {
        ReportReason.Scam or ReportReason.Impersonation => ModerationSeverity.High,
        ReportReason.Harassment or ReportReason.Abuse => ModerationSeverity.Medium,
        _ => ModerationSeverity.Low
    };

    private bool TryGetUserId(out Guid userId) =>
        Guid.TryParse(User.FindFirst("sub")?.Value, out userId);
}

public sealed record CreateReportRequest(Guid ReportedUserId, ReportReason Reason, string? Details);
