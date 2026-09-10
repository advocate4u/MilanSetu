using MilanSetu.Api.Data;
using MilanSetu.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MilanSetu.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/discovery")]
public sealed class DiscoveryController(MilanSetuDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Discover([FromQuery] int? minAge, [FromQuery] int? maxAge, [FromQuery] string? city, [FromQuery] int? religionId, [FromQuery] int? communityId, [FromQuery] int? casteId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        page = Math.Clamp(page, 1, 10000);
        pageSize = Math.Clamp(pageSize, 1, 50);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var currentUserId = GetUserId();
        var query = db.Profiles.AsNoTracking().Where(x => x.UserId != currentUserId && x.Visibility != ProfileVisibility.Hidden);
        if (minAge is >= 18 and <= 100) query = query.Where(x => x.DateOfBirth <= today.AddYears(-minAge.Value));
        if (maxAge is >= 18 and <= 100) query = query.Where(x => x.DateOfBirth > today.AddYears(-maxAge.Value - 1));
        if (!string.IsNullOrWhiteSpace(city)) query = query.Where(x => x.Locations.Any(l => l.Location.CityName == city));
        if (religionId.HasValue) query = query.Where(x => db.ProfileIdentityPreferences.Any(p => p.ProfileId == x.Id && p.ReligionId == religionId));
        if (communityId.HasValue) query = query.Where(x => db.ProfileIdentityPreferences.Any(p => p.ProfileId == x.Id && p.CommunityId == communityId));
        if (casteId.HasValue) query = query.Where(x => db.ProfileIdentityPreferences.Any(p => p.ProfileId == x.Id && p.CasteId == casteId));
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new { x.Id, userId = x.UserId, x.DisplayName, x.DateOfBirth, x.Gender, x.MaritalStatus, x.MotherTongue, x.Bio, city = x.Locations.Where(l => l.IsPrimary).Select(l => l.Location.CityName).FirstOrDefault(), education = x.Education == null ? null : x.Education.HighestQualification, profession = x.Employment == null ? null : x.Employment.Profession })
            .ToListAsync(ct);
        return Ok(new { page, pageSize, total, items });
    }

    private Guid GetUserId() => Guid.TryParse(User.FindFirst("sub")?.Value, out var id) ? id : throw new UnauthorizedAccessException();
}
