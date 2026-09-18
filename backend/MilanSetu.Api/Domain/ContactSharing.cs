namespace MilanSetu.Api.Domain;

public enum ContactShareRequestStatus { Pending = 1, Accepted = 2, Declined = 3 }

public sealed class ContactSharingSettings
{
    public int Id { get; set; } = 1;
    public bool Enabled { get; set; } = true;
    public bool RequireMutualMatch { get; set; } = true;
    public bool SharePhone { get; set; } = true;
    public bool ShareEmail { get; set; } = false;
    public bool ShareWhatsApp { get; set; } = true;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? UpdatedByUserId { get; set; }
}

public sealed class ContactShareRequest
{
    public Guid Id { get; set; }
    public Guid ConnectionId { get; set; }
    public Guid RequesterUserId { get; set; }
    public Guid RecipientUserId { get; set; }
    public ContactShareRequestStatus Status { get; set; } = ContactShareRequestStatus.Pending;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? RespondedAt { get; set; }
    public User Requester { get; set; } = null!;
    public User Recipient { get; set; } = null!;
}