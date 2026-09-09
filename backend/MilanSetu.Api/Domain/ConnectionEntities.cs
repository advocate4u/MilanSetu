namespace MilanSetu.Api.Domain;

public enum InterestStatus
{
    Pending = 1,
    Accepted = 2,
    Declined = 3,
    Cancelled = 4
}

public class Interest
{
    public Guid Id { get; set; }
    public Guid SenderUserId { get; set; }
    public Guid ReceiverUserId { get; set; }
    public InterestStatus Status { get; set; } = InterestStatus.Pending;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? RespondedAt { get; set; }
    public User Sender { get; set; } = null!;
    public User Receiver { get; set; } = null!;
}

public class Connection
{
    public Guid Id { get; set; }
    public Guid UserAId { get; set; }
    public Guid UserBId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public User UserA { get; set; } = null!;
    public User UserB { get; set; } = null!;
}

public class Block
{
    public Guid Id { get; set; }
    public Guid BlockerUserId { get; set; }
    public Guid BlockedUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public User Blocker { get; set; } = null!;
    public User Blocked { get; set; } = null!;
}
