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
    private IRepository<Country> CountriesRepo => unitOfWork.Repository<Country>();
    private IRepository<State> StatesRepo => unitOfWork.Repository<State>();
    private IRepository<District> DistrictsRepo => unitOfWork.Repository<District>();
    private IRepository<City> CitiesRepo => unitOfWork.Repository<City>();
    private IRepository<BloodGroup> BloodGroupsRepo => unitOfWork.Repository<BloodGroup>();
    private IRepository<MaritalStatus> MaritalStatusesRepo => unitOfWork.Repository<MaritalStatus>();
    private IRepository<MotherTongue> MotherTonguesRepo => unitOfWork.Repository<MotherTongue>();
    private IRepository<EducationLevel> EducationLevelsRepo => unitOfWork.Repository<EducationLevel>();
    private IRepository<EmploymentTypeMaster> EmploymentTypesRepo => unitOfWork.Repository<EmploymentTypeMaster>();
    private IRepository<DietType> DietTypesRepo => unitOfWork.Repository<DietType>();
    private IRepository<SmokingStatus> SmokingStatusesRepo => unitOfWork.Repository<SmokingStatus>();
    private IRepository<DrinkingStatus> DrinkingStatusesRepo => unitOfWork.Repository<DrinkingStatus>();

    
    [HttpGet("countries")]
    public async Task<IActionResult> Countries(CancellationToken ct) =>
        Ok(await CountriesRepo.Query().Where(x => x.IsActive).OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
            .Select(x => new { x.Id, x.Code, x.Name }).ToListAsync(ct));

    [HttpGet("states")]
    public async Task<IActionResult> States([FromQuery] int? countryId, CancellationToken ct)
    {
        var q = StatesRepo.Query().Where(x => x.IsActive);
        if (countryId.HasValue) q = q.Where(x => x.CountryId == countryId);
        return Ok(await q.OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
            .Select(x => new { x.Id, x.CountryId, x.Name }).ToListAsync(ct));
    }

    [HttpGet("districts")]
    public async Task<IActionResult> Districts([FromQuery] int? stateId, CancellationToken ct)
    {
        var q = DistrictsRepo.Query().Where(x => x.IsActive);
        if (stateId.HasValue) q = q.Where(x => x.StateId == stateId);
        return Ok(await q.OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
            .Select(x => new { x.Id, x.StateId, x.Name }).ToListAsync(ct));
    }

    [HttpGet("cities")]
    public async Task<IActionResult> Cities([FromQuery] int? districtId, [FromQuery] string? search, CancellationToken ct)
    {
        var q = CitiesRepo.Query().Where(x => x.IsActive);
        if (districtId.HasValue) q = q.Where(x => x.DistrictId == districtId);
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(x => x.Name.Contains(search));
        return Ok(await q.OrderBy(x => x.SortOrder).ThenBy(x => x.Name).Take(100)
            .Select(x => new { x.Id, x.DistrictId, x.Name }).ToListAsync(ct));
    }

    [HttpGet("blood-groups")]
    public async Task<IActionResult> BloodGroups(CancellationToken ct) => Ok(await Master(BloodGroupsRepo, ct));

    [HttpGet("marital-statuses")]
    public async Task<IActionResult> MaritalStatuses(CancellationToken ct) => Ok(await Master(MaritalStatusesRepo, ct));

    [HttpGet("mother-tongues")]
    public async Task<IActionResult> MotherTongues(CancellationToken ct) => Ok(await Master(MotherTonguesRepo, ct));

    [HttpGet("education-levels")]
    public async Task<IActionResult> EducationLevels(CancellationToken ct) => Ok(await Master(EducationLevelsRepo, ct));

    [HttpGet("employment-types")]
    public async Task<IActionResult> EmploymentTypes(CancellationToken ct) => Ok(await Master(EmploymentTypesRepo, ct));

    [HttpGet("diet-types")]
    public async Task<IActionResult> DietTypes(CancellationToken ct) => Ok(await Master(DietTypesRepo, ct));

    [HttpGet("smoking-statuses")]
    public async Task<IActionResult> SmokingStatuses(CancellationToken ct) => Ok(await Master(SmokingStatusesRepo, ct));

    [HttpGet("drinking-statuses")]
    public async Task<IActionResult> DrinkingStatuses(CancellationToken ct) => Ok(await Master(DrinkingStatusesRepo, ct));

    private static async Task<List<object>> Master<T>(IRepository<T> repo, CancellationToken ct) where T : MasterEntity =>
        await repo.Query().Where(x => x.IsActive).OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
            .Select(x => (object)new { x.Id, x.Name }).ToListAsync(ct);

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
