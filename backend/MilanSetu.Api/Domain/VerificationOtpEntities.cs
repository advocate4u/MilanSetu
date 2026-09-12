namespace MilanSetu.Api.Domain;

public enum VerificationChallengePurpose
{
    VerifyMobile = 1,
    VerifyEmail = 2
}

public sealed class VerificationChallenge
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public VerificationChallengePurpose Purpose { get; set; }
    public string CodeHash { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? ConsumedAt { get; set; }
    public int FailedAttempts { get; set; }
    public User User { get; set; } = null!;
}