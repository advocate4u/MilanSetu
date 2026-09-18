using MilanSetu.Api.Domain;
using MilanSetu.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MilanSetu.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/legal")]
public sealed class LegalAcceptanceController(LegalAcceptanceService legalAcceptance, ReviewerAuthorizationService authorization) : ControllerBase
{
    [HttpGet("acceptance")]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var records = await legalAcceptance.GetAsync(userId, cancellationToken);
        return Ok(new
        {
            currentVersion = new
            {
                termsOfUse = LegalAcceptanceService.TermsVersion,
                privacyPolicy = LegalAcceptanceService.PrivacyVersion,
                personalInformationResponsibility = LegalAcceptanceService.PersonalInformationVersion,
                independentProfileVerification = LegalAcceptanceService.VerificationVersion
            },
            accepted = records.Select(x => new
            {
                id = x.Id,
                type = x.AcceptanceType.ToString(),
                version = x.Version,
                acceptedAt = x.AcceptedAt
            }),
            allRequiredAccepted = await legalAcceptance.HasCurrentRequiredAcceptancesAsync(userId, cancellationToken)
        });
    }

    [HttpPost("acceptance")]
    public async Task<IActionResult> Accept(LegalAcceptanceRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (!request.TermsOfUse || !request.PrivacyPolicy ||
            !request.PersonalInformationResponsibility || !request.IndependentProfileVerification)
            return BadRequest(new { message = "All required legal acceptances must be accepted." });

        await legalAcceptance.RecordRequiredAsync(userId, cancellationToken);
        return Ok(new { accepted = true, version = LegalAcceptanceService.TermsVersion });
    }

    [HttpGet("documents")]
    [AllowAnonymous]
    public IActionResult Documents()
        => Ok(new
        {
            termsVersion = LegalAcceptanceService.TermsVersion,
            privacyVersion = LegalAcceptanceService.PrivacyVersion,
            userResponsibility = new
            {
                personalInformation = "Users are solely responsible for the personal and profile information they choose to provide.",
                independentVerification = "Users are responsible for independently verifying identity, background, compatibility and other material information before making significant decisions.",
                platformRole = "MilanSetu provides a technology platform to help people connect and potentially find a partner. It does not guarantee a relationship, marriage, identity, compatibility or user conduct."
            }
        });

    [HttpGet("/admin/users/{userId:guid}/acceptance")]
    public async Task<IActionResult> AdminGet(Guid userId, CancellationToken cancellationToken)
    {
        var actor = await authorization.GetAuthorizedActorAsync(User, cancellationToken);
        if (actor is null || actor.Value.Role != UserRole.Admin) return Forbid();

        var records = await legalAcceptance.GetAsync(userId, cancellationToken);
        return Ok(records.Select(x => new
        {
            id = x.Id,
            userId = x.UserId,
            type = x.AcceptanceType.ToString(),
            version = x.Version,
            acceptedAt = x.AcceptedAt,
            ipAddress = x.IPAddress,
            userAgent = x.UserAgent
        }));
    }

    private bool TryGetUserId(out Guid userId)
        => Guid.TryParse(User.FindFirst("sub")?.Value, out userId);
}
