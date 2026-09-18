namespace MilanSetu.Api.Domain;

public enum AdvertisementMode { Disabled = 0, Sponsor = 1, AdSense = 2 }

public sealed class AdvertisementSettings
{
    public int Id { get; set; } = 1;
    public bool Enabled { get; set; }
    public AdvertisementMode Mode { get; set; } = AdvertisementMode.Disabled;
    public string Label { get; set; } = "Advertisement";
    public string Text { get; set; } = "Help keep MilanSetu free for everyone.";
    public string? TargetUrl { get; set; }
    public string? ImageUrl { get; set; }
    public string? AdSenseClient { get; set; }
    public string? AdSenseSlot { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? UpdatedByUserId { get; set; }
}
