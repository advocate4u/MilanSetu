namespace MilanSetu.Api.Domain;

public sealed class LoginSession
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public DateTimeOffset LoginAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastSeenAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LogoutAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public string? IPAddress { get; set; }
    public string? Country { get; set; }
    public string? State { get; set; }
    public string? City { get; set; }
    public string? DeviceType { get; set; }
    public string? DeviceModel { get; set; }
    public string? DeviceName { get; set; }
    public string? DeviceId { get; set; }
    public string? OS { get; set; }
    public string? OSVersion { get; set; }
    public string? AppVersion { get; set; }
    public string? Browser { get; set; }
    public string? BrowserVersion { get; set; }
    public string LoginProvider { get; set; } = "password";
    public string LoginStatus { get; set; } = "Active";

    public User User { get; set; } = null!;
    public ICollection<SecurityAuditLog> AuditLogs { get; set; } = new List<SecurityAuditLog>();
}

public sealed class SecurityAuditLog
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public Guid? SessionId { get; set; }
    public string EventType { get; set; } = null!;
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public string? IPAddress { get; set; }
    public string? DeviceId { get; set; }
    public string? Provider { get; set; }
    public bool Success { get; set; }
    public string? FailureReason { get; set; }
    public string? Metadata { get; set; }

    public User? User { get; set; }
    public LoginSession? Session { get; set; }
}
