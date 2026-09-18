namespace MilanSetu.Api.Domain;

public sealed class Ignore
{
    public Guid Id { get; set; }
    public Guid IgnorerUserId { get; set; }
    public Guid IgnoredUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public User Ignorer { get; set; } = null!;
    public User Ignored { get; set; } = null!;
}
