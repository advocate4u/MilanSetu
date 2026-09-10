using MilanSetu.Api.Domain;
using Xunit;

namespace MilanSetu.Api.Services;

public sealed class MessageModerationServiceTests
{
    private readonly MessageModerationService service = new();

    [Fact]
    public void AllowsNormalMessage()
    {
        var result = service.Analyze("I enjoy reading and spending time with family.");
        Assert.False(result.RequiresIntervention);
        Assert.Equal(ModerationSeverity.Low, result.Severity);
    }

    [Theory]
    [InlineData("Please send me money for an emergency")]
    [InlineData("send me money")]
    [InlineData("Send.ME.MONEY")]
    public void FlagsHighRiskFinancialRequests(string message)
    {
        var result = service.Analyze(message);
        Assert.True(result.RequiresIntervention);
        Assert.Equal(ModerationSeverity.High, result.Severity);
    }

    [Fact]
    public void FlagsThreats()
    {
        var result = service.Analyze("I will hurt you");
        Assert.True(result.RequiresIntervention);
        Assert.Equal(ModerationSeverity.High, result.Severity);
    }

    [Fact]
    public void NormalizesUnicodeAndObfuscation()
    {
        var result = service.Analyze("Please send   me... money!!!");
        Assert.True(result.RequiresIntervention);
        Assert.Equal(ModerationSeverity.High, result.Severity);
    }
}
