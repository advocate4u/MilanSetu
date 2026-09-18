namespace MilanSetu.Api.Domain;

public enum UserRole
{
    User = 1,
    Reviewer = 2,
    Admin = 3,
    SuperAdmin = 4
}

public sealed class UserRoleAssignment
{
    public Guid UserId { get; set; }
    public UserRole Role { get; set; } = UserRole.User;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    public User User { get; set; } = null!;
}

public sealed class AuditLog
{
    public Guid Id { get; set; }
    public Guid? ActorUserId { get; set; }
    public string Action { get; set; } = null!;
    public string ResourceType { get; set; } = null!;
    public Guid? ResourceId { get; set; }
    public string? Metadata { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public User? ActorUser { get; set; }
}
