using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MilanSetu.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/profile/photos")]
public sealed class ProfilePhotosController(IWebHostEnvironment environment) : ControllerBase
{
    private const long MaxFileSize = 5 * 1024 * 1024;
    private const int MaxPhotos = 6;
    private static readonly string[] AllowedContentTypes = ["image/jpeg", "image/png", "image/webp"];

    [HttpGet]
    public IActionResult List()
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var directory = UserDirectory(userId);
        if (!Directory.Exists(directory)) return Ok(Array.Empty<object>());

        var files = Directory.EnumerateFiles(directory)
            .Select(Path.GetFileName)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .OrderBy(x => x, StringComparer.Ordinal)
            .Select(name => new { fileName = name, url = $"/api/profile/photos/{Uri.EscapeDataString(name!)}" })
            .ToList();

        return Ok(files);
    }

    [HttpPost]
    [RequestSizeLimit(MaxFileSize + 64 * 1024)]
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (file is null || file.Length == 0) return BadRequest(new { message = "Please select a photo." });
        if (file.Length > MaxFileSize) return BadRequest(new { message = "Photo must be 5 MB or smaller." });
        if (!AllowedContentTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
            return BadRequest(new { message = "Only JPEG, PNG, and WebP photos are supported." });

        var directory = UserDirectory(userId);
        Directory.CreateDirectory(directory);
        var currentCount = Directory.EnumerateFiles(directory).Count();
        if (currentCount >= MaxPhotos) return Conflict(new { message = "You can add up to 6 profile photos." });

        await using var input = file.OpenReadStream();
        var header = new byte[12];
        var read = await input.ReadAsync(header.AsMemory(0, header.Length), ct);
        if (!LooksLikeImage(header, read, file.ContentType))
            return BadRequest(new { message = "The selected file is not a supported image." });

        var extension = file.ContentType.ToLowerInvariant() switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            _ => ".webp"
        };
        var fileName = $"{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}_{Guid.NewGuid():N}{extension}";
        var path = Path.Combine(directory, fileName);

        await using var output = System.IO.File.Create(path);
        input.Position = 0;
        await input.CopyToAsync(output, ct);

        return Created($"/api/profile/photos/{Uri.EscapeDataString(fileName)}", new
        {
            fileName,
            url = $"/api/profile/photos/{Uri.EscapeDataString(fileName)}"
        });
    }

    [HttpGet("{fileName}")]
    public IActionResult Get(string fileName)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (!IsSafeFileName(fileName)) return BadRequest();

        var path = Path.Combine(UserDirectory(userId), fileName);
        if (!System.IO.File.Exists(path)) return NotFound();

        var contentType = Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".jpg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
        return PhysicalFile(path, contentType, enableRangeProcessing: true);
    }

    [HttpDelete("{fileName}")]
    public IActionResult Delete(string fileName)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (!IsSafeFileName(fileName)) return BadRequest();

        var path = Path.Combine(UserDirectory(userId), fileName);
        if (!System.IO.File.Exists(path)) return NotFound();
        System.IO.File.Delete(path);
        return NoContent();
    }

    private string UserDirectory(Guid userId) =>
        Path.Combine(environment.ContentRootPath, "App_Data", "profile-photos", userId.ToString("N"));

    private bool TryGetUserId(out Guid userId) => Guid.TryParse(User.FindFirst("sub")?.Value, out userId);

    private static bool IsSafeFileName(string fileName) =>
        !string.IsNullOrWhiteSpace(fileName) && Path.GetFileName(fileName) == fileName && fileName.Length <= 180;

    private static bool LooksLikeImage(byte[] header, int read, string contentType)
    {
        if (contentType.Equals("image/jpeg", StringComparison.OrdinalIgnoreCase))
            return read >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;
        if (contentType.Equals("image/png", StringComparison.OrdinalIgnoreCase))
            return read >= 8 && header.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        return read >= 12 && header.AsSpan(0, 4).SequenceEqual("RIFF"u8) && header.AsSpan(8, 4).SequenceEqual("WEBP"u8);
    }
}
