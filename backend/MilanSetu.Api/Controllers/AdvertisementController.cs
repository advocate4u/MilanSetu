using System.Text.Json;
using MilanSetu.Api.Data;
using MilanSetu.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MilanSetu.Api.Controllers;

[ApiController]
[Route("api/advertising")]
public sealed class AdvertisementController(MilanSetuDbContext db) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var ad = await db.AdvertisementSettings.AsNoTracking().SingleOrDefaultAsync(x => x.Id == 1, ct);
        if (ad is null || !ad.Enabled || ad.Mode == AdvertisementMode.Disabled) return Ok(new { enabled = false });
        return Ok(new { enabled = true, mode = ad.Mode.ToString(), label = ad.Label, text = ad.Text, targetUrl = ad.TargetUrl, imageUrl = ad.ImageUrl, adSenseClient = ad.Mode == AdvertisementMode.AdSense ? ad.AdSenseClient : null, adSenseSlot = ad.Mode == AdvertisementMode.AdSense ? ad.AdSenseSlot : null });
    }

    [Authorize]
    [HttpGet("admin")]
    public async Task<IActionResult> GetAdmin(CancellationToken ct)
    {
        if (!await IsSuperAdmin(ct)) return Forbid();
        var ad = await db.AdvertisementSettings.AsNoTracking().SingleOrDefaultAsync(x => x.Id == 1, ct) ?? new AdvertisementSettings();
        return Ok(ad);
    }

    [Authorize]
    [HttpPut("admin")]
    public async Task<IActionResult> Update(AdvertisementSettingsRequest input, CancellationToken ct)
    {
        if (!await IsSuperAdmin(ct)) return Forbid();
        if (!Enum.IsDefined(input.Mode)) return BadRequest(new { message = "Invalid advertisement mode." });
        if (input.Label?.Length > 80 || input.Text?.Length > 500 || input.TargetUrl?.Length > 1000 || input.ImageUrl?.Length > 1000 || input.AdSenseClient?.Length > 200 || input.AdSenseSlot?.Length > 100) return BadRequest(new { message = "Advertisement configuration is too long." });
        if (input.Enabled && input.Mode == AdvertisementMode.Sponsor && string.IsNullOrWhiteSpace(input.Text)) return BadRequest(new { message = "Sponsor text is required." });
        if (input.Enabled && input.Mode == AdvertisementMode.AdSense && (string.IsNullOrWhiteSpace(input.AdSenseClient) || string.IsNullOrWhiteSpace(input.AdSenseSlot))) return BadRequest(new { message = "AdSense client and slot are required when AdSense is enabled." });
        if (!string.IsNullOrWhiteSpace(input.TargetUrl) && !Uri.TryCreate(input.TargetUrl, UriKind.Absolute, out var target) || target?.Scheme is not ("https" or "http")) return BadRequest(new { message = "Target URL must be an absolute HTTP(S) URL." });
        if (!string.IsNullOrWhiteSpace(input.ImageUrl) && (!Uri.TryCreate(input.ImageUrl, UriKind.Absolute, out var image) || image?.Scheme is not ("https" or "http"))) return BadRequest(new { message = "Image URL must be an absolute HTTP(S) URL." });
        var actorId = Guid.Parse(User.FindFirst("sub")!.Value);
        var ad = await db.AdvertisementSettings.SingleOrDefaultAsync(x => x.Id == 1, ct);
        if (ad is null) { ad = new AdvertisementSettings { Id = 1 }; db.AdvertisementSettings.Add(ad); }
        ad.Enabled = input.Enabled; ad.Mode = input.Mode; ad.Label = input.Label?.Trim() ?? "Advertisement"; ad.Text = input.Text?.Trim() ?? ""; ad.TargetUrl = input.TargetUrl?.Trim(); ad.ImageUrl = input.ImageUrl?.Trim(); ad.AdSenseClient = input.AdSenseClient?.Trim(); ad.AdSenseSlot = input.AdSenseSlot?.Trim(); ad.UpdatedAt = DateTimeOffset.UtcNow; ad.UpdatedByUserId = actorId;
        db.AuditLogs.Add(new AuditLog { Id = Guid.NewGuid(), ActorUserId = actorId, Action = "superadmin.advertising.update", ResourceType = "AdvertisementSettings", ResourceId = Guid.Empty, Metadata = JsonSerializer.Serialize(new { ad.Enabled, ad.Mode, ad.Label }), CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync(ct);
        return Ok(ad);
    }

    private async Task<bool> IsSuperAdmin(CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var id)) return false;
        return await db.UserRoleAssignments.AsNoTracking().AnyAsync(x => x.UserId == id && x.Role == UserRole.SuperAdmin, ct);
    }
}

public sealed record AdvertisementSettingsRequest(bool Enabled, AdvertisementMode Mode, string? Label, string? Text, string? TargetUrl, string? ImageUrl, string? AdSenseClient, string? AdSenseSlot);
