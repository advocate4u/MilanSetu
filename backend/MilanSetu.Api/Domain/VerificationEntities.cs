namespace MilanSetu.Api.Domain;

public enum VerificationType
{
    Mobile = 1,
    Email = 2,
    Identity = 3,
    Education = 4,
    Employment = 5
}

public enum VerificationStatus
{
    NotStarted = 1,
    Pending = 2,
    Verified = 3,
    Rejected = 4,
    Expired = 5
}

public sealed class VerificationRequest
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public VerificationType Type { get; set; }
    public VerificationStatus Status { get; set; } = VerificationStatus.Pending;
    public DateTimeOffset RequestedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ReviewedAt { get; set; }
    public DateTimeOffset? VerifiedAt { get; set; }
    public string? ReviewerNotes { get; set; }
    public User User { get; set; } = null!;
}

public sealed class VerificationChallenge
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid VerificationRequestId { get; set; }
    public VerificationType Type { get; set; }
    public string Destination { get; set; } = null!;
    public string CodeHash { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? ConsumedAt { get; set; }
    public DateTimeOffset? VerifiedAt { get; set; }
    public int FailedAttempts { get; set; }
    public DateTimeOffset? LastAttemptAt { get; set; }
    public User User { get; set; } = null!;
    public VerificationRequest VerificationRequest { get; set; } = null!;
}
