using MilanSetu.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace MilanSetu.Api.Controllers;

[ApiController]
[Route("api/auth")]
[EnableRateLimiting("auth")]
public sealed class AuthController(AuthService authService, IConfiguration configuration) : ControllerBase
{
    private const string DefaultCookieName = "milansetu_refresh";

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "Email and password are required." });

        if (request.Password.Length < 12)
            return BadRequest(new { message = "Password must be at least 12 characters." });

        if (request.Email.Length > 320)
            return BadRequest(new { message = "Email is too long." });

        try
        {
            var user = await authService.RegisterAsync(request.Email, request.Password, cancellationToken);
            return Created("/api/auth/me", new { userId = user.Id, email = user.Email });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var tokens = await authService.SignInAsync(request.Email, request.Password, cancellationToken);
            SetRefreshCookie(tokens.RefreshToken);
            return Ok(new { accessToken = tokens.AccessToken, expiresInSeconds = 900 });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies[CookieName];
        if (string.IsNullOrWhiteSpace(refreshToken))
            return Unauthorized(new { message = "Refresh token is missing." });

        try
        {
            var tokens = await authService.RefreshAsync(refreshToken, cancellationToken);
            SetRefreshCookie(tokens.RefreshToken);
            return Ok(new { accessToken = tokens.AccessToken, expiresInSeconds = 900 });
        }
        catch (UnauthorizedAccessException)
        {
            ClearRefreshCookie();
            return Unauthorized(new { message = "Session expired. Please sign in again." });
        }
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies[CookieName];
        if (!string.IsNullOrWhiteSpace(refreshToken))
            await authService.RevokeAsync(refreshToken, cancellationToken);

        ClearRefreshCookie();
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me() => Ok(new
    {
        userId = User.FindFirst("sub")?.Value,
        email = User.FindFirst("email")?.Value
    });

    private string CookieName => configuration["Auth:RefreshTokenCookieName"] ?? DefaultCookieName;

    private void SetRefreshCookie(string token)
    {
        Response.Cookies.Append(CookieName, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = configuration.GetValue("Auth:RefreshTokenCookieSecure", !HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment()),
            SameSite = SameSiteMode.Strict,
            Path = "/api/auth",
            MaxAge = TimeSpan.FromDays(30)
        });
    }

    private void ClearRefreshCookie()
    {
        Response.Cookies.Delete(CookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = configuration.GetValue("Auth:RefreshTokenCookieSecure", !HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment()),
            SameSite = SameSiteMode.Strict,
            Path = "/api/auth"
        });
    }
}

public sealed record RegisterRequest(string Email, string Password);
public sealed record LoginRequest(string Email, string Password);
