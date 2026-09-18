using MilanSetu.Api.Domain;

namespace MilanSetu.Api.Services;

public sealed class ModerationAutomationService
{
    private static readonly string[] HighRiskSignals =
    [
        "upi", "bank account", "bank transfer", "send money", "wire money", "gift card",
        "otp", "one time password", "password", "crypto", "bitcoin", "investment", "loan",
        "urgent payment", "pay me", "advance payment", "fee", "verification code"
    ];

    private static readonly string[] MediumRiskSignals =
    [
        "threat", "blackmail", "abuse", "harass", "insult", "explicit", "nude",
        "private photo", "secret", "meet alone", "force"
    ];

    public ModerationAssessment Assess(ReportReason reason, string? details, int reportsAgainstTargetLast30Days)
    {
        var text = MessageModerationService.Normalize(details ?? string.Empty);
        var score = reason switch
        {
            ReportReason.Scam or ReportReason.Impersonation => 80,
            ReportReason.Harassment or ReportReason.Abuse => 50,
            ReportReason.InappropriateContent => 35,
            _ => 20
        };

        if (HighRiskSignals.Any(text.Contains)) score += 30;
        else if (MediumRiskSignals.Any(text.Contains)) score += 15;
        if (reportsAgainstTargetLast30Days >= 3) score += 20;
        if (reportsAgainstTargetLast30Days >= 5) score += 20;

        var severity = score >= 80 ? ModerationSeverity.High : score >= 45 ? ModerationSeverity.Medium : ModerationSeverity.Low;
        var status = severity == ModerationSeverity.High ? ModerationStatus.Reviewing : ModerationStatus.Open;
        return new ModerationAssessment(severity, status, score, reportsAgainstTargetLast30Days);
    }
}

public sealed record ModerationAssessment(ModerationSeverity Severity, ModerationStatus Status, int RiskScore, int ReportsAgainstTargetLast30Days);