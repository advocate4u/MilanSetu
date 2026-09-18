using MilanSetu.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace MilanSetu.Api.Controllers;

[ApiController]
[Route("api/auth")]
[EnableRateLimiting("auth")]
public sealed class AuthController(AuthService authService, ExternalAuthService externalAuthService, SecurityAuditService securityAudit, ReviewerAuthorizationService reviewerAuthorization, IConfiguration configuration) : ControllerBase
{
    private const string DefaultCookieName = "milansetu_refresh";

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password)) return BadRequest(new { message = "Email and password are required." });
        if (request.Email.Length > 320) return BadRequest(new { message = "Email is too long." });
        if (request.Password.Length < 12) return BadRequest(new { message = "Password must be at least 12 characters." });
        if (request.Password.Length > 128) return BadRequest(new { message = "Password is too long." });
        if (request.Email.Length > 320) return BadRequest(new { message = "Email is too long." });
        try { var user = await authService.RegisterAsync(request.Email, request.Password, cancellationToken); return Created("/api/auth/me", new { userId = user.Id, email = user.Email }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "Email and password are required." });
        if (request.Email.Length > 320 || request.Password.Length > 128)
            return BadRequest(new { message = "Email or password is too long." });
        var context = securityAudit.Capture();
        try
        {
            var tokens = await authService.SignInAsync(request.Email, request.Password, context, cancellationToken);
            await securityAudit.RecordAsync(UserIdFromEmail(request.Email), tokens.SessionId, "LOGIN_SUCCESS", true, "password", context, cancellationToken: cancellationToken);
            SetRefreshCookie(tokens.RefreshToken);
            return Ok(new { accessToken = tokens.AccessToken, expiresInSeconds = 900, sessionId = tokens.SessionId });
        }
        catch (UnauthorizedAccessException)
        {
            await securityAudit.RecordAsync(null, null, "LOGIN_FAILED", false, "password", context, "INVALID_CREDENTIALS", cancellationToken: cancellationToken);
            return Unauthorized(new { message = "Invalid email or password." });
        }
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies[CookieName];
        if (string.IsNullOrWhiteSpace(refreshToken)) return Unauthorized(new { message = "Refresh token is missing." });
        var context = securityAudit.Capture();
        try
        {
            var tokens = await authService.RefreshAsync(refreshToken, context, cancellationToken);
            await securityAudit.RecordAsync(Guid.TryParse(User.FindFirst("sub")?.Value, out var uid) ? uid : null, tokens.SessionId, "TOKEN_REFRESH", true, "refresh", context, cancellationToken: cancellationToken);
            SetRefreshCookie(tokens.RefreshToken);
            return Ok(new { accessToken = tokens.AccessToken, expiresInSeconds = 900, sessionId = tokens.SessionId });
        }
        catch (UnauthorizedAccessException)
        {
            await securityAudit.RecordAsync(null, null, "TOKEN_REFRESH_FAILED", false, "refresh", context, "INVALID_OR_EXPIRED_TOKEN", cancellationToken: cancellationToken);
            ClearRefreshCookie();
            return Unauthorized(new { message = "Session expired. Please sign in again." });
        }
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies[CookieName];
        if (!string.IsNullOrWhiteSpace(refreshToken)) await authService.RevokeAsync(refreshToken, cancellationToken);
        var context = securityAudit.Capture();
        var userId = Guid.TryParse(User.FindFirst("sub")?.Value, out var uid) ? uid : (Guid?)null;
        var sessionId = Guid.TryParse(User.FindFirst("sid")?.Value, out var sid) ? sid : (Guid?)null;
        if (sessionId is Guid currentSession) await securityAudit.EndSessionAsync(currentSession, false, cancellationToken);
        await securityAudit.RecordAsync(userId, sessionId, "LOGOUT", true, "session", context, cancellationToken: cancellationToken);
        ClearRefreshCookie(); return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var role = await reviewerAuthorization.GetRoleAsync(User, cancellationToken);
        return Ok(new { userId = User.FindFirst("sub")?.Value, email = User.FindFirst("email")?.Value, role = role?.ToString() ?? "User" });
    }

    private Guid? UserIdFromEmail(string email) => null;

    private string CookieName => configuration["Auth:RefreshTokenCookieName"] ?? DefaultCookieName;
    private void SetRefreshCookie(string token) => Response.Cookies.Append(CookieName, token, new CookieOptions { HttpOnly = true, Secure = configuration.GetValue("Auth:RefreshTokenCookieSecure", !HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment()), SameSite = SameSiteMode.Strict, Path = "/api/auth", MaxAge = TimeSpan.FromDays(30) });
    private void ClearRefreshCookie() => Response.Cookies.Delete(CookieName, new CookieOptions { HttpOnly = true, Secure = configuration.GetValue("Auth:RefreshTokenCookieSecure", !HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment()), SameSite = SameSiteMode.Strict, Path = "/api/auth" });
}

public sealed record RegisterRequest(string Email, string Password);
public sealed record LoginRequest(string Email, string Password);

public sealed record ExternalLoginRequest(string Credential);
