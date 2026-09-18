namespace MilanSetu.Api.Domain;

public enum LegalAcceptanceType
{
    TermsOfUse,
    PrivacyPolicy,
    PersonalInformationResponsibility,
    IndependentProfileVerification
}

public sealed class LegalAcceptance
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public LegalAcceptanceType AcceptanceType { get; set; }
    public string Version { get; set; } = null!;
    public DateTimeOffset AcceptedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? IPAddress { get; set; }
    public string? UserAgent { get; set; }

    public User User { get; set; } = null!;
}