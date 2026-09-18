using MilanSetu.Api.Data.Repositories;
using MilanSetu.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace MilanSetu.Api.Services;

public sealed class LegalAcceptanceService(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
{
    private IRepository<LegalAcceptance> Acceptances => unitOfWork.Repository<LegalAcceptance>();

    public const string TermsVersion = "2026-09-18";
    public const string PrivacyVersion = "2026-09-18";
    public const string PersonalInformationVersion = "2026-09-18";
    public const string VerificationVersion = "2026-09-18";

    public async Task RecordRequiredAsync(Guid userId, CancellationToken cancellationToken)
    {
        await RecordAsync(userId, LegalAcceptanceType.TermsOfUse, TermsVersion, cancellationToken);
        await RecordAsync(userId, LegalAcceptanceType.PrivacyPolicy, PrivacyVersion, cancellationToken);
        await RecordAsync(userId, LegalAcceptanceType.PersonalInformationResponsibility, PersonalInformationVersion, cancellationToken);
        await RecordAsync(userId, LegalAcceptanceType.IndependentProfileVerification, VerificationVersion, cancellationToken);
    }

    public async Task<bool> HasCurrentRequiredAcceptancesAsync(Guid userId, CancellationToken cancellationToken)
    {
        var required = new[]
        {
            (LegalAcceptanceType.TermsOfUse, TermsVersion),
            (LegalAcceptanceType.PrivacyPolicy, PrivacyVersion),
            (LegalAcceptanceType.PersonalInformationResponsibility, PersonalInformationVersion),
            (LegalAcceptanceType.IndependentProfileVerification, VerificationVersion)
        };

        var accepted = await Acceptances.Query(false)
            .Where(x => x.UserId == userId)
            .Select(x => new { x.AcceptanceType, x.Version })
            .ToListAsync(cancellationToken);

        return required.All(r => accepted.Any(x => x.AcceptanceType == r.Item1 && x.Version == r.Item2));
    }

    public async Task RecordAsync(Guid userId, LegalAcceptanceType type, string version, CancellationToken cancellationToken)
    {
        var exists = await Acceptances.SingleOrDefaultAsync(
            x => x.UserId == userId && x.AcceptanceType == type && x.Version == version,
            cancellationToken);

        if (exists is not null) return;

        var request = httpContextAccessor.HttpContext?.Request;
        var ip = request?.HttpContext.Connection.RemoteIpAddress?.ToString();
        if (request?.HttpContext.Connection.RemoteIpAddress?.IsIPv4MappedToIPv6 == true)
            ip = request.HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();

        var userAgent = request?.Headers.UserAgent.ToString();
        if (userAgent?.Length > 1000) userAgent = userAgent[..1000];

        Acceptances.Add(new LegalAcceptance
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            AcceptanceType = type,
            Version = version,
            AcceptedAt = DateTimeOffset.UtcNow,
            IPAddress = ip,
            UserAgent = string.IsNullOrWhiteSpace(userAgent) ? null : userAgent
        });

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LegalAcceptance>> GetAsync(Guid userId, CancellationToken cancellationToken)
        => await Acceptances.Query(false)
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.AcceptedAt)
            .ToListAsync(cancellationToken);
}

public sealed record LegalAcceptanceRequest(
    bool TermsOfUse,
    bool PrivacyPolicy,
    bool PersonalInformationResponsibility,
    bool IndependentProfileVerification);
