using MilanSetu.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace MilanSetu.Api.Controllers;
[ApiController]
[Route("api/taxonomy")]
public class TaxonomyController(MilanSetuDbContext db):ControllerBase
{
 [HttpGet("religions")]
 public async Task<IActionResult> Religions(CancellationToken ct)=>Ok(await db.Religions.AsNoTracking().Where(x=>x.IsActive).OrderBy(x=>x.Name).Select(x=>new{x.Id,x.Name}).ToListAsync(ct));
 [HttpGet("communities")]
 public async Task<IActionResult> Communities([FromQuery]int? religionId,CancellationToken ct){var q=db.Communities.AsNoTracking().Where(x=>x.IsActive);if(religionId.HasValue)q=q.Where(x=>x.ReligionId==religionId);return Ok(await q.OrderBy(x=>x.Name).Select(x=>new{x.Id,x.Name,x.ReligionId}).ToListAsync(ct));}
 [HttpGet("castes")]
 public async Task<IActionResult> Castes([FromQuery]int? communityId,CancellationToken ct){var q=db.Castes.AsNoTracking().Where(x=>x.IsActive);if(communityId.HasValue)q=q.Where(x=>x.CommunityId==communityId);return Ok(await q.OrderBy(x=>x.Name).Select(x=>new{x.Id,x.Name,x.CommunityId}).ToListAsync(ct));}
 [HttpGet("locations")]
 public async Task<IActionResult> Locations([FromQuery]string? countryCode,[FromQuery]string? state,[FromQuery]string? district,[FromQuery]string? search,CancellationToken ct){var q=db.Locations.AsNoTracking().AsQueryable();if(!string.IsNullOrWhiteSpace(countryCode))q=q.Where(x=>x.CountryCode==countryCode);if(!string.IsNullOrWhiteSpace(state))q=q.Where(x=>x.StateName==state);if(!string.IsNullOrWhiteSpace(district))q=q.Where(x=>x.DistrictName==district);if(!string.IsNullOrWhiteSpace(search))q=q.Where(x=>EF.Functions.ILike(x.CityName,$"%{search}%"));return Ok(await q.OrderBy(x=>x.CityName).Take(100).Select(x=>new{x.Id,x.CountryCode,x.StateName,x.DistrictName,x.CityName}).ToListAsync(ct));}
}