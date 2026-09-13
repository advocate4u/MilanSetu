namespace MilanSetu.Api.Domain;

public sealed class ReviewerAuditEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ReviewerUserId { get; set; }
    public Guid VerificationRequestId { get; set; }
    public VerificationType VerificationType { get; set; }
    public string Action { get; set; } = null!;
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
