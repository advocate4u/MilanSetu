namespace MilanSetu.Api.Domain;

public enum NotificationType
{
    InterestReceived = 1,
    InterestAccepted = 2,
    MessageReceived = 3,
    SafetyNotice = 4
}

public sealed class Notification
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public NotificationType Type { get; set; }
    public string Title { get; set; } = null!;
    public string Body { get; set; } = null!;
    public Guid? RelatedUserId { get; set; }
    public Guid? RelatedEntityId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ReadAt { get; set; }
    public User User { get; set; } = null!;
}
