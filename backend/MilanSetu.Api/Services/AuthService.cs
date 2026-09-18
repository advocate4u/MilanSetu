using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using MilanSetu.Api.Data.Repositories;
using MilanSetu.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace MilanSetu.Api.Services;

public sealed class AuthService(IUnitOfWork unitOfWork, IConfiguration configuration)
{
    private const int PasswordIterations = 210_000;
    private const int PasswordKeyLength = 32;
    private const int RefreshTokenBytes = 32;

    private IRepository<User> Users => unitOfWork.Repository<User>();
    private IRepository<RefreshToken> RefreshTokens => unitOfWork.Repository<RefreshToken>();

    public async Task<User> RegisterAsync(string email, string password, CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        if (await Users.AnyAsync(x => x.Email == normalizedEmail, cancellationToken))
            throw new InvalidOperationException("An account with this email already exists.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = normalizedEmail,
            PasswordHash = HashPassword(password),
            IsActive = true
        };

        Users.Add(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return user;
    }

    public async Task<(string AccessToken, string RefreshToken)> SignInAsync(string email, string password, CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await Users.SingleOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken);
        if (user is null || !user.IsActive || !VerifyPassword(password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password.");

        return await IssueTokensAsync(user, cancellationToken);
    }

    public async Task<(string AccessToken, string RefreshToken)> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var hash = HashToken(refreshToken);
        var current = await RefreshTokens.SingleOrDefaultAsync(x => x.TokenHash == hash, cancellationToken);
        if (current is null || current.RevokedAt is not null || current.ExpiresAt <= DateTimeOffset.UtcNow)
            throw new UnauthorizedAccessException("Invalid or expired refresh token.");

        var user = await Users.SingleOrDefaultAsync(x => x.Id == current.UserId, cancellationToken);
        if (user is null || !user.IsActive)
            throw new UnauthorizedAccessException("Account is not active.");

        var tokens = await IssueTokensAsync(user, cancellationToken);
        var replacement = await RefreshTokens.SingleOrDefaultAsync(x => x.TokenHash == HashToken(tokens.RefreshToken), cancellationToken);
        if (replacement is null)
            throw new InvalidOperationException("Unable to create replacement refresh token.");

        current.RevokedAt = DateTimeOffset.UtcNow;
        current.ReplacedByTokenId = replacement.Id;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return tokens;
    }

    public Task<(string AccessToken, string RefreshToken)> IssueTokensForExternalLoginAsync(User user, CancellationToken cancellationToken)
        => IssueTokensAsync(user, cancellationToken);

    public async Task RevokeAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var token = await RefreshTokens.SingleOrDefaultAsync(x => x.TokenHash == HashToken(refreshToken), cancellationToken);
        if (token is not null && token.RevokedAt is null)
        {
            token.RevokedAt = DateTimeOffset.UtcNow;
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<(string AccessToken, string RefreshToken)> IssueTokensAsync(User user, CancellationToken cancellationToken)
    {
        var jwtKey = configuration["Auth:Jwt:Key"];
        if (string.IsNullOrWhiteSpace(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < 32)
            throw new InvalidOperationException("Auth:Jwt:Key must be configured with at least 256 bits of entropy.");

        var now = DateTimeOffset.UtcNow;
        var accessToken = CreateAccessToken(user, now, jwtKey);
        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(RefreshTokenBytes));

        RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = HashToken(refreshToken),
            CreatedAt = now,
            ExpiresAt = now.AddDays(30)
        });
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return (accessToken, refreshToken);
    }

    private string CreateAccessToken(User user, DateTimeOffset now, string key)
    {
        var issuer = configuration["Auth:Jwt:Issuer"] ?? "MilanSetu";
        var audience = configuration["Auth:Jwt:Audience"] ?? "MilanSetu.Web";
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(issuer, audience, claims, now.UtcDateTime, now.AddMinutes(15).UtcDateTime, credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, PasswordIterations, HashAlgorithmName.SHA256, PasswordKeyLength);
        return "PBKDF2-SHA256$v1$" + PasswordIterations + "$" + Convert.ToBase64String(salt) + "$" + Convert.ToBase64String(hash);
    }

    private static bool VerifyPassword(string password, string encoded)
    {
        var parts = encoded.Split('$');
        if (parts.Length != 5 || parts[0] != "PBKDF2-SHA256" || parts[1] != "v1" || !int.TryParse(parts[2], out var iterations))
            return false;

        try
        {
            var salt = Convert.FromBase64String(parts[3]);
            var expected = Convert.FromBase64String(parts[4]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
}
