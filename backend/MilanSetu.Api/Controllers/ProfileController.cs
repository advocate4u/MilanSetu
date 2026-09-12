using MilanSetu.Api.Data;
using MilanSetu.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MilanSetu.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/profile")]
public sealed class ProfileController(MilanSetuDbContext db) : ControllerBase
{
    [HttpGet("me")]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var profile = await db.Profiles.AsNoTracking()
            .Include(x => x.Locations).ThenInclude(x => x.Location)
            .Include(x => x.Preferences)
            .Include(x => x.Education)
            .Include(x => x.Employment)
            .Include(x => x.FamilyDetails)
            .Include(x => x.Lifestyle)
            .SingleOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        return profile is null ? NotFound(new { message = "Profile has not been created yet." }) : Ok(ToResponse(profile));
    }

    [HttpPut("me/identity")]
    public async Task<IActionResult> UpdateIdentity(ProfileIdentityRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var profile = await db.Profiles.SingleOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        if (profile is null) return NotFound(new { message = "Create your profile first." });
        if (request.ReligionId.HasValue && !await db.Religions.AnyAsync(x => x.Id == request.ReligionId && x.IsActive, cancellationToken)) return BadRequest(new { message = "Invalid religion." });
        if (request.CommunityId.HasValue && !await db.Communities.AnyAsync(x => x.Id == request.CommunityId && x.IsActive && (!request.ReligionId.HasValue || x.ReligionId == request.ReligionId), cancellationToken)) return BadRequest(new { message = "Community does not belong to the selected religion." });
        if (request.CasteId.HasValue && !await db.Castes.AnyAsync(x => x.Id == request.CasteId && x.IsActive && (!request.CommunityId.HasValue || x.CommunityId == request.CommunityId), cancellationToken)) return BadRequest(new { message = "Caste does not belong to the selected community." });
        if (!Enum.IsDefined(request.Importance)) return BadRequest(new { message = "Invalid preference importance." });
        var value = await db.ProfileIdentityPreferences.SingleOrDefaultAsync(x => x.ProfileId == profile.Id, cancellationToken);
        if (value is null) { value = new ProfileIdentityPreference { ProfileId = profile.Id }; db.ProfileIdentityPreferences.Add(value); }
        value.ReligionId = request.ReligionId; value.CommunityId = request.CommunityId; value.CasteId = request.CasteId; value.Importance = request.Importance;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Identity preferences saved." });
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpsertMine(ProfileRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (request.DisplayName?.Trim().Length is not > 0 or > 120) return BadRequest(new { message = "Display name is required and must be 120 characters or fewer." });
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (request.DateOfBirth > today.AddYears(-18)) return BadRequest(new { message = "MilanSetu profiles must be for adults aged 18 or above." });
        if (request.DateOfBirth < today.AddYears(-100)) return BadRequest(new { message = "Please provide a valid date of birth." });
        if (!Enum.IsDefined(request.Gender)) return BadRequest(new { message = "A valid gender is required." });
        if (!Enum.IsDefined(request.AccountType)) return BadRequest(new { message = "A valid account type is required." });
        if (request.MinPartnerAge is < 18 or > 100 || request.MaxPartnerAge is < 18 or > 100 || (request.MinPartnerAge.HasValue && request.MaxPartnerAge.HasValue && request.MinPartnerAge > request.MaxPartnerAge)) return BadRequest(new { message = "Please provide a valid partner age range." });
        var profile = await db.Profiles.SingleOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        if (profile is null) { profile = new Profile { Id = Guid.NewGuid(), UserId = userId, CreatedAt = DateTimeOffset.UtcNow }; db.Profiles.Add(profile); }
        profile.DisplayName = request.DisplayName.Trim(); profile.DateOfBirth = request.DateOfBirth; profile.Gender = request.Gender; profile.AccountType = request.AccountType;
        profile.MaritalStatus = Clean(request.MaritalStatus, 40); profile.MotherTongue = Clean(request.MotherTongue, 80); profile.Bio = Clean(request.Bio, 2000); profile.Visibility = request.Visibility; profile.UpdatedAt = DateTimeOffset.UtcNow;
        var preference = await db.ProfilePreferences.SingleOrDefaultAsync(x => x.ProfileId == profile.Id, cancellationToken);
        if (preference is null) { preference = new ProfilePreference { Id = Guid.NewGuid(), ProfileId = profile.Id }; db.ProfilePreferences.Add(preference); }
        preference.MinAge = request.MinPartnerAge; preference.MaxAge = request.MaxPartnerAge; preference.RelocationOpen = request.RelocationOpen;
        preference.Importance = Enum.TryParse<PreferenceImportance>(request.PreferenceImportance, true, out var importance) ? importance : PreferenceImportance.Flexible;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { profileId = profile.Id, message = "Profile saved." });
    }

    [HttpPut("me/extended")]
    public async Task<IActionResult> UpdateExtended(ProfileExtendedRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var profile = await db.Profiles.SingleOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        if (profile is null) return NotFound(new { message = "Create your profile first." });

        var education = await db.Educations.SingleOrDefaultAsync(x => x.ProfileId == profile.Id, cancellationToken);
        if (education is null) { education = new Education { ProfileId = profile.Id }; db.Educations.Add(education); }
        education.HighestQualification = Clean(request.HighestQualification, 160);
        education.FieldOfStudy = Clean(request.FieldOfStudy, 160);
        education.Institution = Clean(request.Institution, 200);

        var employment = await db.Employments.SingleOrDefaultAsync(x => x.ProfileId == profile.Id, cancellationToken);
        if (employment is null) { employment = new Employment { ProfileId = profile.Id }; db.Employments.Add(employment); }
        employment.Profession = Clean(request.Profession, 160);
        employment.Industry = Clean(request.Industry, 160);
        employment.EmploymentType = Clean(request.EmploymentType, 80);
        employment.WorkLocation = Clean(request.WorkLocation, 160);

        var family = await db.FamilyDetails.SingleOrDefaultAsync(x => x.ProfileId == profile.Id, cancellationToken);
        if (family is null) { family = new FamilyDetails { ProfileId = profile.Id }; db.FamilyDetails.Add(family); }
        family.ParentsStatus = Clean(request.ParentsStatus, 120);
        family.SiblingsSummary = Clean(request.SiblingsSummary, 500);
        family.FamilyLocation = Clean(request.FamilyLocation, 160);
        family.FamilyStructure = Clean(request.FamilyStructure, 120);

        var lifestyle = await db.Lifestyles.SingleOrDefaultAsync(x => x.ProfileId == profile.Id, cancellationToken);
        if (lifestyle is null) { lifestyle = new Lifestyle { ProfileId = profile.Id }; db.Lifestyles.Add(lifestyle); }
        lifestyle.FoodPreference = Clean(request.FoodPreference, 80);
        lifestyle.Smoking = Clean(request.Smoking, 80);
        lifestyle.Alcohol = Clean(request.Alcohol, 80);
        lifestyle.Exercise = Clean(request.Exercise, 80);
        lifestyle.Interests = Clean(request.Interests, 500);
        lifestyle.Travel = Clean(request.Travel, 120);
        lifestyle.Pets = Clean(request.Pets, 120);

        profile.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Extended profile information saved." });
    }

    private Guid GetUserId() => Guid.TryParse(User.FindFirst("sub")?.Value, out var id) ? id : throw new UnauthorizedAccessException();
    private static string? Clean(string? value, int maxLength) { if (string.IsNullOrWhiteSpace(value)) return null; var trimmed = value.Trim(); return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength]; }
    private static object ToResponse(Profile profile) => new { profile.Id, profile.DisplayName, profile.DateOfBirth, profile.Gender, profile.AccountType, profile.MaritalStatus, profile.MotherTongue, profile.Bio, profile.Visibility, locations = profile.Locations.Select(x => new { x.LocationId, x.IsPrimary, x.Location.CountryCode, x.Location.StateName, x.Location.DistrictName, x.Location.CityName }), preferences = profile.Preferences.Select(x => new { x.MinAge, x.MaxAge, x.RelocationOpen, x.Importance }), education = profile.Education == null ? null : new { profile.Education.HighestQualification, profile.Education.FieldOfStudy, profile.Education.Institution }, employment = profile.Employment == null ? null : new { profile.Employment.Profession, profile.Employment.Industry, profile.Employment.EmploymentType, profile.Employment.WorkLocation }, family = profile.FamilyDetails == null ? null : new { profile.FamilyDetails.ParentsStatus, profile.FamilyDetails.SiblingsSummary, profile.FamilyDetails.FamilyLocation, profile.FamilyDetails.FamilyStructure }, lifestyle = profile.Lifestyle == null ? null : new { profile.Lifestyle.FoodPreference, profile.Lifestyle.Smoking, profile.Lifestyle.Alcohol, profile.Lifestyle.Exercise, profile.Lifestyle.Interests, profile.Lifestyle.Travel, profile.Lifestyle.Pets });
}

public sealed record ProfileIdentityRequest(int? ReligionId, int? CommunityId, int? CasteId, PreferenceImportance Importance);
public sealed record ProfileRequest(string? DisplayName, DateOnly DateOfBirth, Gender Gender, AccountType AccountType, string? MaritalStatus, string? MotherTongue, string? Bio, ProfileVisibility Visibility, int? MinPartnerAge, int? MaxPartnerAge, bool? RelocationOpen, string? PreferenceImportance);
public sealed record ProfileExtendedRequest(string? HighestQualification, string? FieldOfStudy, string? Institution, string? Profession, string? Industry, string? EmploymentType, string? WorkLocation, string? ParentsStatus, string? SiblingsSummary, string? FamilyLocation, string? FamilyStructure, string? FoodPreference, string? Smoking, string? Alcohol, string? Exercise, string? Interests, string? Travel, string? Pets);
