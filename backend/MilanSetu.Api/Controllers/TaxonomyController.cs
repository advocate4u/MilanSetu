using MilanSetu.Api.Data.Repositories;
using MilanSetu.Api.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MilanSetu.Api.Controllers;

[ApiController]
[Route("api/taxonomy")]
public sealed class TaxonomyController(IUnitOfWork unitOfWork) : ControllerBase
{
    private IRepository<Religion> ReligionsRepo => unitOfWork.Repository<Religion>();
    private IRepository<Community> CommunitiesRepo => unitOfWork.Repository<Community>();
    private IRepository<Caste> CastesRepo => unitOfWork.Repository<Caste>();
    private IRepository<Location> LocationsRepo => unitOfWork.Repository<Location>();

    [HttpGet("religions")]
    public async Task<IActionResult> Religions(CancellationToken ct) =>
        Ok(await ReligionsRepo.Query().Where(x => x.IsActive).OrderBy(x => x.Name).Select(x => new { x.Id, x.Name }).ToListAsync(ct));

    [HttpGet("communities")]
    public async Task<IActionResult> Communities([FromQuery] int? religionId, CancellationToken ct)
    {
        var q = CommunitiesRepo.Query().Where(x => x.IsActive);
        if (religionId.HasValue) q = q.Where(x => x.ReligionId == religionId);
        return Ok(await q.OrderBy(x => x.Name).Select(x => new { x.Id, x.Name, x.ReligionId }).ToListAsync(ct));
    }

    [HttpGet("castes")]
    public async Task<IActionResult> Castes([FromQuery] int? communityId, CancellationToken ct)
    {
        var q = CastesRepo.Query().Where(x => x.IsActive);
        if (communityId.HasValue) q = q.Where(x => x.CommunityId == communityId);
        return Ok(await q.OrderBy(x => x.Name).Select(x => new { x.Id, x.Name, x.CommunityId }).ToListAsync(ct));
    }

    [HttpGet("locations")]
    public async Task<IActionResult> Locations(
        [FromQuery] string? countryCode,
        [FromQuery] string? state,
        [FromQuery] string? district,
        [FromQuery] string? search,
        CancellationToken ct)
    {
        var q = LocationsRepo.Query();
        if (!string.IsNullOrWhiteSpace(countryCode)) q = q.Where(x => x.CountryCode == countryCode);
        if (!string.IsNullOrWhiteSpace(state)) q = q.Where(x => x.StateName == state);
        if (!string.IsNullOrWhiteSpace(district)) q = q.Where(x => x.DistrictName == district);
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(x => x.CityName.Contains(search));
        return Ok(await q.OrderBy(x => x.CityName).Take(100)
            .Select(x => new { x.Id, x.CountryCode, x.StateName, x.DistrictName, x.CityName })
            .ToListAsync(ct));
    }
}
