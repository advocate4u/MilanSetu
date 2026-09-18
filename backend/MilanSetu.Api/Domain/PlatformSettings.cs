namespace MilanSetu.Api.Domain;

public sealed class PlatformSettings
{
    public int Id { get; set; } = 1;
    public bool MessagingEnabled { get; set; } = true;
    public int MaxProfilePhotos { get; set; } = 6;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? UpdatedByUserId { get; set; }
}