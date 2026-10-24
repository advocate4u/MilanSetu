using System.Diagnostics;
using System.Security.Cryptography;

namespace MilanSetu.Api.Services;

public interface IVerificationDocumentStorage
{
    Task<string> SaveQuarantineAsync(Stream content, string storageKey, CancellationToken ct);
    Task<Stream> OpenReadAsync(string storageKey, CancellationToken ct);
    Task DeleteAsync(string storageKey, CancellationToken ct);
}

public sealed class FileSystemVerificationDocumentStorage(IConfiguration configuration) : IVerificationDocumentStorage
{
    private string Root => Path.GetFullPath(configuration["VerificationDocuments:RootPath"] ?? Path.Combine(AppContext.BaseDirectory, "private-verification-documents"));

    public async Task<string> SaveQuarantineAsync(Stream content, string storageKey, CancellationToken ct)
    {
        var path = GetPath(storageKey);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var target = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 64 * 1024, useAsync: true);
        await content.CopyToAsync(target, ct);
        return path;
    }

    public Task<Stream> OpenReadAsync(string storageKey, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        Stream stream = new FileStream(GetPath(storageKey), FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, useAsync: true);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string storageKey, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var path = GetPath(storageKey);
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    private string GetPath(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey) || Path.IsPathRooted(storageKey) ||
            storageKey.Split('/', '\\\\').Any(part => part is "" or "." or ".."))
            throw new InvalidOperationException("Invalid storage key.");

        var root = Root;
        var path = Path.GetFullPath(Path.Combine(root, storageKey.Replace('/', Path.DirectorySeparatorChar)));
        var rootPrefix = root.EndsWith(Path.DirectorySeparatorChar) ? root : root + Path.DirectorySeparatorChar;
        if (!path.StartsWith(rootPrefix, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            throw new InvalidOperationException("Invalid storage key.");
        return path;
    }
}

public interface IVerificationDocumentScanner
{
    Task<(bool Safe, string Verdict)> ScanAsync(string physicalPath, CancellationToken ct);
}

/// <summary>
/// Fail-closed ClamAV adapter. Set VerificationDocuments:ClamAv:ExecutablePath to the
/// installed clamscan executable. Missing scanner or scanner errors never mark a file safe.
/// </summary>
public sealed class ClamAvVerificationDocumentScanner(IConfiguration configuration) : IVerificationDocumentScanner
{
    public async Task<(bool Safe, string Verdict)> ScanAsync(string physicalPath, CancellationToken ct)
    {
        var executable = configuration["VerificationDocuments:ClamAv:ExecutablePath"];
        if (string.IsNullOrWhiteSpace(executable) || !File.Exists(executable))
            return (false, "ScannerNotConfigured");

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = executable,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            },
            EnableRaisingEvents = true
        };
        process.StartInfo.ArgumentList.Add("--no-summary");
        process.StartInfo.ArgumentList.Add(physicalPath);

        try
        {
            if (!process.Start()) return (false, "ScannerStartFailed");
            var stdout = process.StandardOutput.ReadToEndAsync(ct);
            var stderr = process.StandardError.ReadToEndAsync(ct);
            await process.WaitForExitAsync(ct);
            await Task.WhenAll(stdout, stderr);

            return process.ExitCode switch
            {
                0 => (true, "Clean"),
                1 => (false, "MalwareDetected"),
                _ => (false, "ScannerError")
            };
        }
        catch (OperationCanceledException)
        {
            try { if (!process.HasExited) process.Kill(entireProcessTree: true); } catch (InvalidOperationException) { }
            throw;
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            return (false, "ScannerError");
        }
    }
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
        var extensionTypeMatches = extension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => string.Equals(contentType, "image/jpeg", StringComparison.OrdinalIgnoreCase),
            ".png" => string.Equals(contentType, "image/png", StringComparison.OrdinalIgnoreCase),
            ".pdf" => string.Equals(contentType, "application/pdf", StringComparison.OrdinalIgnoreCase),
            _ => false
        };
        if (!AllowedExtensions.Contains(extension) || !AllowedContentTypes.Contains(contentType) ||
            !extensionTypeMatches || length <= 0 || length > MaxBytes)
            throw new InvalidOperationException("Only non-empty JPG, PNG or PDF files up to 10 MB are accepted.");
    }

    public static async Task<bool> HasValidSignatureAsync(Stream stream, string contentType, CancellationToken ct)
    {
        if (!stream.CanSeek) return false;
        stream.Position = 0;
        var header = new byte[8];
        var read = await stream.ReadAsync(header.AsMemory(0, header.Length), ct);
        stream.Position = 0;
        return contentType.ToLowerInvariant() switch
        {
            "image/jpeg" => read >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
            "image/png" => read >= 8 && header.SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
            "application/pdf" => read >= 5 && header[0] == 0x25 && header[1] == 0x50 && header[2] == 0x44 && header[3] == 0x46 && header[4] == 0x2D,
            _ => false
        };
    }

    public static async Task<string> ComputeSha256Async(Stream stream, CancellationToken ct)
    {
        if (stream.CanSeek) stream.Position = 0;
        var hash = await SHA256.HashDataAsync(stream, ct);
        if (stream.CanSeek) stream.Position = 0;
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
