namespace MilanSetu.Api.Domain;

public enum VerificationDocumentStatus
{
    Quarantined = 1,
    Scanning = 2,
    Available = 3,
    Rejected = 4,
    Deleted = 5
}

public sealed class VerificationDocument
{
    public Guid Id { get; set; }
    public Guid VerificationRequestId { get; set; }
    public Guid UserId { get; set; }
    public string OriginalFileName { get; set; } = null!;
    public string StorageKey { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public long SizeBytes { get; set; }
    public string Sha256 { get; set; } = null!;
    public VerificationDocumentStatus Status { get; set; } = VerificationDocumentStatus.Quarantined;
    public DateTimeOffset UploadedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ScannedAt { get; set; }
    public string? ScannerVerdict { get; set; }
    public VerificationRequest VerificationRequest { get; set; } = null!;
    public User User { get; set; } = null!;
}
