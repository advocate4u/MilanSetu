using System.Security.Cryptography;

namespace MilanSetu.Api.Services;

public interface IVerificationDocumentStorage
{
    Task<string> SaveQuarantineAsync(Stream content, string storageKey, CancellationToken ct);
    Task DeleteAsync(string storageKey, CancellationToken ct);
}

public sealed class FileSystemVerificationDocumentStorage(IConfiguration configuration) : IVerificationDocumentStorage
{
    private string Root => configuration["VerificationDocuments:RootPath"] ?? Path.Combine(AppContext.BaseDirectory, "private-verification-documents");

    public async Task<string> SaveQuarantineAsync(Stream content, string storageKey, CancellationToken ct)
    {
        var path = GetPath(storageKey);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var target = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 64 * 1024, useAsync: true);
        await content.CopyToAsync(target, ct);
        return path;
    }

    public Task DeleteAsync(string storageKey, CancellationToken ct)
    {
        var path = GetPath(storageKey);
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    private string GetPath(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey) || storageKey.Contains("..", StringComparison.Ordinal) || Path.IsPathRooted(storageKey)) throw new InvalidOperationException("Invalid storage key.");
        return Path.Combine(Root, storageKey.Replace('/', Path.DirectorySeparatorChar));
    }
}

public interface IVerificationDocumentScanner
{
    Task<(bool Safe, string Verdict)> ScanAsync(string physicalPath, CancellationToken ct);
}

public sealed class QuarantineOnlyVerificationDocumentScanner : IVerificationDocumentScanner
{
    public Task<(bool Safe, string Verdict)> ScanAsync(string physicalPath, CancellationToken ct)
        => Task.FromResult((false, "ScannerNotConfigured"));
}

public static class VerificationDocumentSecurity
{
    public const long MaxBytes = 10 * 1024 * 1024;
    public static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".pdf" };
    public static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase) { "image/jpeg", "image/png", "application/pdf" };

    public static string NormalizeFileName(string name)
    {
        var safe = Path.GetFileName(name ?? string.Empty).Trim();
        if (safe.Length == 0 || safe.Length > 180) throw new InvalidOperationException("Invalid file name.");
        return safe;
    }

    public static void Validate(string fileName, string contentType, long length)
    {
        var extension = Path.GetExtension(fileName);
        if (!AllowedExtensions.Contains(extension) || !AllowedContentTypes.Contains(contentType) || length <= 0 || length > MaxBytes)
            throw new InvalidOperationException("Only non-empty JPG, PNG or PDF files up to 10 MB are accepted.");
    }

    public static async Task<string> ComputeSha256Async(Stream stream, CancellationToken ct)
    {
        if (stream.CanSeek) stream.Position = 0;
        var hash = await SHA256.HashDataAsync(stream, ct);
        if (stream.CanSeek) stream.Position = 0;
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
