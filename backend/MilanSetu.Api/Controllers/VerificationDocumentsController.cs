using System.Security.Claims;
using MilanSetu.Api.Data;
using MilanSetu.Api.Domain;
using MilanSetu.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MilanSetu.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/verification/documents")]
public sealed class VerificationDocumentsController(MilanSetuDbContext db, IVerificationDocumentStorage storage) : ControllerBase
{
    [HttpPost("{verificationRequestId:guid}")]
    [RequestSizeLimit(11 * 1024 * 1024)]
    public async Task<IActionResult> Upload(Guid verificationRequestId, IFormFile? file, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var request = await db.VerificationRequests.SingleOrDefaultAsync(x => x.Id == verificationRequestId && x.UserId == userId && (x.Type == VerificationType.Identity || x.Type == VerificationType.Education || x.Type == VerificationType.Employment), ct);
        if (request is null) return NotFound();
        if (request.Status != VerificationStatus.Pending) return Conflict(new { message = "A document can only be uploaded for a pending verification request." });
        if (file is null) return BadRequest(new { message = "A verification document is required." });
        string fileName;
        try { fileName = VerificationDocumentSecurity.NormalizeFileName(file.FileName); VerificationDocumentSecurity.Validate(fileName, file.ContentType, file.Length); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        await using var input = file.OpenReadStream();
        var hash = await VerificationDocumentSecurity.ComputeSha256Async(input, ct);
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var documentId = Guid.NewGuid();
        var storageKey = $"quarantine/{userId:N}/{documentId:N}{extension}";
        try
        {
            await storage.SaveQuarantineAsync(input, storageKey, ct);
            var document = new VerificationDocument { Id = documentId, VerificationRequestId = request.Id, UserId = userId, OriginalFileName = fileName, StorageKey = storageKey, ContentType = file.ContentType, SizeBytes = file.Length, Sha256 = hash, Status = VerificationDocumentStatus.Quarantined };
            db.Set<VerificationDocument>().Add(document);
            await db.SaveChangesAsync(ct);
            return Accepted(new { documentId, status = document.Status.ToString(), message = "Document uploaded to private quarantine. It is not available to reviewers until malware scanning is configured and passes." });
        }
        catch { await storage.DeleteAsync(storageKey, ct); throw; }
    }

    [HttpGet("mine")]
    public async Task<IActionResult> Mine(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var documents = await db.Set<VerificationDocument>().AsNoTracking().Where(x => x.UserId == userId && x.Status != VerificationDocumentStatus.Deleted).OrderByDescending(x => x.UploadedAt).Select(x => new { x.Id, x.VerificationRequestId, x.OriginalFileName, x.ContentType, x.SizeBytes, status = x.Status.ToString(), x.UploadedAt, x.ScannedAt, x.ScannerVerdict }).ToListAsync(ct);
        return Ok(documents);
    }

    private bool TryGetUserId(out Guid userId) => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out userId);
}
