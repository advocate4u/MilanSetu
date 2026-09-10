using System.Text;
using System.Text.RegularExpressions;
using MilanSetu.Api.Domain;

namespace MilanSetu.Api.Services;

public sealed record MessageModerationResult(bool RequiresIntervention, ModerationSeverity Severity, string? Reason);

public sealed class MessageModerationService
{
    private static readonly string[] HighRiskPatterns =
    [
        "send me money", "send money", "transfer money", "pay me", "loan me",
        "bank account", "credit card", "debit card", "upi id", "otp", "one time password",
        "threat", "kill you", "hurt you"
    ];

    private static readonly string[] MediumRiskPatterns =
    [
        "cash", "investment", "crypto", "password", "pin", "abuse", "idiot", "stupid"
    ];

    public MessageModerationResult Analyze(string message)
    {
        var normalized = Normalize(message);
        if (HighRiskPatterns.Any(normalized.Contains))
            return new(true, ModerationSeverity.High, "This message contains a possible scam, coercion, or threat signal.");

        if (MediumRiskPatterns.Any(normalized.Contains))
            return new(true, ModerationSeverity.Medium, "This message may contain language or content that could make the conversation unsafe.");

        return new(false, ModerationSeverity.Low, null);
    }

    internal static string Normalize(string value)
    {
        var text = value.Normalize(NormalizationForm.FormKC).ToLowerInvariant();
        text = Regex.Replace(text, @"[^\p{L}\p{Nd}]+", " ");
        return Regex.Replace(text, @"\s+", " ").Trim();
    }
}
