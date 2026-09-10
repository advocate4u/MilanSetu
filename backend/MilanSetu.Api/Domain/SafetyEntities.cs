namespace MilanSetu.Api.Domain;

public enum ReportReason
{
    Abuse = 1,
    Harassment = 2,
    Scam = 3,
    Impersonation = 4,
    InappropriateContent = 5,
    Other = 6
}

public enum ReportStatus
{
    Open = 1,
    Reviewing = 2,
    Resolved = 3,
    Dismissed = 4
}

public class UserReport
{
    public Guid Id { get; set; }
    public Guid ReporterUserId { get; set; }
    public Guid ReportedUserId { get; set; }
    public ReportReason Reason { get; set; }
    public string? Details { get; set; }
    public ReportStatus Status { get; set; } = ReportStatus.Open;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ResolvedAt { get; set; }
    public User Reporter { get; set; } = null!;
    public User ReportedUser { get; set; } = null!;
}
