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
public sealed class VerificationDocumentsController(
    MilanSetuDbContext db,
    IVerificationDocumentStorage storage,
    IVerificationDocumentScanner scanner) : ControllerBase
{
    [HttpPost("{verificationRequestId:guid}")]
    [RequestSizeLimit(11 * 1024 * 1024)]
    public async Task<IActionResult> Upload(Guid verificationRequestId, IFormFile? file, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var request = await db.VerificationRequests.SingleOrDefaultAsync(x =>
            x.Id == verificationRequestId && x.UserId == userId &&
            (x.Type == VerificationType.Identity || x.Type == VerificationType.Education || x.Type == VerificationType.Employment), ct);
        if (request is null) return NotFound();
        if (request.Status != VerificationStatus.Pending) return Conflict(new { message = "A document can only be uploaded for a pending verification request." });
        if (file is null) return BadRequest(new { message = "A verification document is required." });

        string fileName;
        try { fileName = VerificationDocumentSecurity.NormalizeFileName(file.FileName); VerificationDocumentSecurity.Validate(fileName, file.ContentType, file.Length); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }

        await using var input = file.OpenReadStream();
        if (!await VerificationDocumentSecurity.HasValidSignatureAsync(input, file.ContentType, ct))
            return BadRequest(new { message = "The file content does not match the declared image or PDF type." });

        var hash = await VerificationDocumentSecurity.ComputeSha256Async(input, ct);
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var documentId = Guid.NewGuid();
        var storageKey = $"quarantine/{userId:N}/{documentId:N}{extension}";
        try
        {
            var physicalPath = await storage.SaveQuarantineAsync(input, storageKey, ct);
            var (safe, verdict) = await scanner.ScanAsync(physicalPath, ct);
            var status = safe
                ? VerificationDocumentStatus.Available
                : verdict == "MalwareDetected" ? VerificationDocumentStatus.Rejected : VerificationDocumentStatus.Quarantined;

            var document = new VerificationDocument
            {
                Id = documentId,
                VerificationRequestId = request.Id,
                UserId = userId,
                OriginalFileName = fileName,
                StorageKey = storageKey,
                ContentType = file.ContentType,
                SizeBytes = file.Length,
                Sha256 = hash,
                Status = status,
                ScannedAt = verdict == "ScannerNotConfigured" ? null : DateTimeOffset.UtcNow,
                ScannerVerdict = verdict
            };
            db.Set<VerificationDocument>().Add(document);
            await db.SaveChangesAsync(ct);

            var message = status switch
            {
                VerificationDocumentStatus.Available => "Document passed malware scanning and is available to authorized reviewers.",
                VerificationDocumentStatus.Rejected => "The file was rejected by malware scanning. Please upload a clean document.",
                _ => "Document is stored privately in quarantine and will not be available to reviewers until malware scanning succeeds."
            };
            return Accepted(new { documentId, status = status.ToString(), scannerVerdict = verdict, message });
        }
        catch
        {
            await storage.DeleteAsync(storageKey, CancellationToken.None);
            throw;
        }
    }

    [HttpGet("mine")]
    public async Task<IActionResult> Mine(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var documents = await db.Set<VerificationDocument>().AsNoTracking()
            .Where(x => x.UserId == userId && x.Status != VerificationDocumentStatus.Deleted)
            .OrderByDescending(x => x.UploadedAt)
            .Select(x => new { x.Id, x.VerificationRequestId, x.OriginalFileName, x.ContentType, x.SizeBytes, status = x.Status.ToString(), x.UploadedAt, x.ScannedAt, x.ScannerVerdict })
            .ToListAsync(ct);
        return Ok(documents);
    }

    private bool TryGetUserId(out Guid userId) =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out userId);
}
