using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using MilanSetu.Api.Data.Repositories;
using MilanSetu.Api.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace MilanSetu.Api.Services;

public sealed class SecurityAuditService(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
{
    private IRepository<User> Users => unitOfWork.Repository<User>();
    private IRepository<RefreshToken> RefreshTokens => unitOfWork.Repository<RefreshToken>();
    private IRepository<LoginSession> Sessions => unitOfWork.Repository<LoginSession>();
    private IRepository<SecurityAuditLog> Logs => unitOfWork.Repository<SecurityAuditLog>();

    public ClientSecurityContext Capture()
    {
        var request = httpContextAccessor.HttpContext?.Request;
        var headers = request?.Headers;
        var userAgent = headers?.UserAgent.ToString() ?? string.Empty;
        var platform = Clean(headers?["X-Platform"].ToString(), 40);
        var deviceModel = Clean(headers?["X-Device-Model"].ToString(), 120);
        var deviceName = Clean(headers?["X-Device-Name"].ToString(), 120);
        var deviceId = Clean(headers?["X-Device-Id"].ToString(), 160);
        var appVersion = Clean(headers?["X-App-Version"].ToString(), 40);
        var osVersion = Clean(headers?["X-OS-Version"].ToString(), 40);
        var deviceType = !string.IsNullOrWhiteSpace(platform) ? platform : DetectDeviceType(userAgent);
        var os = !string.IsNullOrWhiteSpace(platform) ? platform : DetectOs(userAgent);
        var browser = DetectBrowser(userAgent, out var browserVersion);
        return new ClientSecurityContext(
            GetClientIp(request),
            deviceType,
            deviceModel,
            deviceName,
            deviceId,
            os,
            osVersion,
            appVersion,
            browser,
            browserVersion);
    }

    public async Task<LoginSession> CreateSessionAsync(
        Guid userId,
        string provider,
        ClientSecurityContext context,
        CancellationToken cancellationToken)
    {
        var session = new LoginSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            LoginAt = DateTimeOffset.UtcNow,
            LastSeenAt = DateTimeOffset.UtcNow,
            IPAddress = context.IPAddress,
            DeviceType = context.DeviceType,
            DeviceModel = context.DeviceModel,
            DeviceName = context.DeviceName,
            DeviceId = context.DeviceId,
            OS = context.OS,
            OSVersion = context.OSVersion,
            AppVersion = context.AppVersion,
            Browser = context.Browser,
            BrowserVersion = context.BrowserVersion,
            LoginProvider = provider,
            LoginStatus = "Active"
        };
        Sessions.Add(session);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return session;
    }

    public async Task RecordAsync(
        Guid? userId,
        Guid? sessionId,
        string eventType,
        bool success,
        string? provider,
        ClientSecurityContext context,
        string? failureReason = null,
        object? metadata = null,
        CancellationToken cancellationToken = default)
    {
        Logs.Add(new SecurityAuditLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SessionId = sessionId,
            EventType = eventType,
            Timestamp = DateTimeOffset.UtcNow,
            IPAddress = context.IPAddress,
            DeviceId = context.DeviceId,
            Provider = provider,
            Success = success,
            FailureReason = Clean(failureReason, 200),
            Metadata = metadata is null ? null : JsonSerializer.Serialize(metadata)
        });
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public Task<Guid?> FindUserIdByEmailAsync(string email, CancellationToken cancellationToken)
        => Users.Query().Where(x => x.Email == email.Trim().ToLowerInvariant()).Select(x => (Guid?)x.Id).SingleOrDefaultAsync(cancellationToken);

    public Task<Guid?> GetSessionUserIdAsync(Guid sessionId, CancellationToken cancellationToken)
        => Sessions.Query().Where(x => x.Id == sessionId).Select(x => (Guid?)x.UserId).SingleOrDefaultAsync(cancellationToken);

    public async Task<LoginSession?> GetSessionEntityAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken)
        => await Sessions.Query(false).SingleOrDefaultAsync(x => x.Id == sessionId && x.UserId == userId, cancellationToken);

    public async Task TouchSessionAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await Sessions.SingleOrDefaultAsync(x => x.Id == sessionId, cancellationToken);
        if (session is not null && session.RevokedAt is null && session.LogoutAt is null)
        {
            session.LastSeenAt = DateTimeOffset.UtcNow;
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task EndSessionAsync(Guid sessionId, bool revoked, CancellationToken cancellationToken)
    {
        var session = await Sessions.SingleOrDefaultAsync(x => x.Id == sessionId, cancellationToken);
        if (session is null) return;
        session.LastSeenAt = DateTimeOffset.UtcNow;
        session.LoginStatus = revoked ? "Revoked" : "LoggedOut";
        if (revoked) session.RevokedAt = DateTimeOffset.UtcNow;
        else session.LogoutAt = DateTimeOffset.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LoginSession>> GetSessionsAsync(Guid userId, CancellationToken cancellationToken)
        => await Sessions.Query()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.LastSeenAt)
            .Take(50)
            .ToListAsync(cancellationToken);

    public async Task<bool> RevokeSessionAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await Sessions.SingleOrDefaultAsync(x => x.Id == sessionId && x.UserId == userId, cancellationToken);
        if (session is null) return false;
        if (session.RevokedAt is null)
        {
            session.RevokedAt = DateTimeOffset.UtcNow;
            session.LoginStatus = "Revoked";
            var tokens = await RefreshTokens.Query(false).Where(x => x.SessionId == sessionId && x.RevokedAt == null).ToListAsync(cancellationToken);
            foreach (var token in tokens) token.RevokedAt = DateTimeOffset.UtcNow;
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        return true;
    }

    private static string? Clean(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var cleaned = value.Trim().Replace("\r", string.Empty).Replace("\n", string.Empty);
        return cleaned.Length <= max ? cleaned : cleaned[..max];
    }

    private static string? GetClientIp(HttpRequest? request)
    {
        if (request is null) return null;
        var ip = request.HttpContext.Connection.RemoteIpAddress;
        return ip is null ? null : (ip.IsIPv4MappedToIPv6 ? ip.MapToIPv4().ToString() : ip.ToString());
    }

    private static string DetectDeviceType(string userAgent)
        => Regex.IsMatch(userAgent, "Mobile|Android|iPhone|iPad", RegexOptions.IgnoreCase) ? "Mobile" : "Web";

    private static string DetectOs(string userAgent)
    {
        if (Regex.IsMatch(userAgent, "Android", RegexOptions.IgnoreCase)) return "Android";
        if (Regex.IsMatch(userAgent, "iPhone|iPad|iOS", RegexOptions.IgnoreCase)) return "iOS";
        if (Regex.IsMatch(userAgent, "Windows", RegexOptions.IgnoreCase)) return "Windows";
        if (Regex.IsMatch(userAgent, "Mac OS X", RegexOptions.IgnoreCase)) return "macOS";
        if (Regex.IsMatch(userAgent, "Linux", RegexOptions.IgnoreCase)) return "Linux";
        return "Unknown";
    }

    private static string? DetectBrowser(string userAgent, out string? version)
    {
        version = null;
        var patterns = new (string Name, string Pattern)[]
        {
            ("Edge", @"Edg/([\d\.]+)"),
            ("Chrome", @"(?:Chrome|CriOS)/([\d\.]+)"),
            ("Firefox", @"(?:Firefox|FxiOS)/([\d\.]+)"),
            ("Safari", @"Version/([\d\.]+).*Safari"),
            ("Samsung Internet", @"SamsungBrowser/([\d\.]+)")
        };
        foreach (var (name, pattern) in patterns)
        {
            var match = Regex.Match(userAgent, pattern, RegexOptions.IgnoreCase);
            if (match.Success) { version = match.Groups[1].Value; return name; }
        }
        return null;
    }
}

public sealed record ClientSecurityContext(
    string? IPAddress,
    string? DeviceType,
    string? DeviceModel,
    string? DeviceName,
    string? DeviceId,
    string? OS,
    string? OSVersion,
    string? AppVersion,
    string? Browser,
    string? BrowserVersion);
