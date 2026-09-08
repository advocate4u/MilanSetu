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
        var profile = await db.Profiles
            .AsNoTracking()
            .Include(x => x.Locations).ThenInclude(x => x.Location)
            .Include(x => x.Preferences)
            .SingleOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        return profile is null ? NotFound(new { message = "Profile has not been created yet." }) : Ok(ToResponse(profile));
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpsertMine(ProfileRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (request.DisplayName?.Trim().Length is not > 0 or > 120)
            return BadRequest(new { message = "Display name is required and must be 120 characters or fewer." });

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (request.DateOfBirth > today.AddYears(-18))
            return BadRequest(new { message = "MilanSetu profiles must be for adults aged 18 or above." });
        if (request.DateOfBirth < today.AddYears(-100))
            return BadRequest(new { message = "Please provide a valid date of birth." });
        if (!Enum.IsDefined(request.Gender))
            return BadRequest(new { message = "A valid gender is required." });
        if (!Enum.IsDefined(request.AccountType))
            return BadRequest(new { message = "A valid account type is required." });

        var profile = await db.Profiles.SingleOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        if (profile is null)
        {
            profile = new Profile { Id = Guid.NewGuid(), UserId = userId, CreatedAt = DateTimeOffset.UtcNow };
            db.Profiles.Add(profile);
        }

        profile.DisplayName = request.DisplayName.Trim();
        profile.DateOfBirth = request.DateOfBirth;
        profile.Gender = request.Gender;
        profile.AccountType = request.AccountType;
        profile.MaritalStatus = Clean(request.MaritalStatus, 40);
        profile.MotherTongue = Clean(request.MotherTongue, 80);
        profile.Bio = Clean(request.Bio, 2000);
        profile.Visibility = request.Visibility;
        profile.UpdatedAt = DateTimeOffset.UtcNow;

        var preference = await db.ProfilePreferences.SingleOrDefaultAsync(x => x.ProfileId == profile.Id, cancellationToken);
        if (preference is null)
        {
            preference = new ProfilePreference { Id = Guid.NewGuid(), ProfileId = profile.Id };
            db.ProfilePreferences.Add(preference);
        }
        preference.MinAge = request.MinPartnerAge;
        preference.MaxAge = request.MaxPartnerAge;
        preference.RelocationOpen = request.RelocationOpen;
        preference.Importance = Clean(request.PreferenceImportance, 20) ?? "Flexible";

        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { profileId = profile.Id, message = "Profile saved." });
    }

    private Guid GetUserId()
    {
        var value = User.FindFirst("sub")?.Value;
        return Guid.TryParse(value, out var id) ? id : throw new UnauthorizedAccessException();
    }

    private static string? Clean(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    private static object ToResponse(Profile profile) => new
    {
        profile.Id,
        profile.DisplayName,
        profile.DateOfBirth,
        profile.Gender,
        profile.AccountType,
        profile.MaritalStatus,
        profile.MotherTongue,
        profile.Bio,
        profile.Visibility,
        locations = profile.Locations.Select(x => new
        {
            x.LocationId,
            x.IsPrimary,
            x.Location.CountryCode,
            x.Location.StateName,
            x.Location.DistrictName,
            x.Location.CityName
        }),
        preferences = profile.Preferences.Select(x => new
        {
            x.MinAge,
            x.MaxAge,
            x.RelocationOpen,
            x.Importance
        })
    };
}

public sealed record ProfileRequest(
    string? DisplayName,
    DateOnly DateOfBirth,
    Gender Gender,
    AccountType AccountType,
    string? MaritalStatus,
    string? MotherTongue,
    string? Bio,
    ProfileVisibility Visibility,
    int? MinPartnerAge,
    int? MaxPartnerAge,
    bool? RelocationOpen,
    string? PreferenceImportance);
