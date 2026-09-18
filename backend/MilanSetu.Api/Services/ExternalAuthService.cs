using System.Text.Json;
using System.Text.Json.Serialization;
using Google.Apis.Auth;
using MilanSetu.Api.Data.Repositories;
using MilanSetu.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace MilanSetu.Api.Services;

public sealed class ExternalAuthService(
    IUnitOfWork unitOfWork,
    IConfiguration configuration,
    IHttpClientFactory httpClientFactory,
    AuthService authService)
{
    private IRepository<User> Users => unitOfWork.Repository<User>();
    private IRepository<ExternalLogin> ExternalLogins => unitOfWork.Repository<ExternalLogin>();

    public async Task<ExternalAuthResult> SignInAsync(
        string provider,
        string credential,
        ClientSecurityContext context,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(credential))
            throw new InvalidOperationException("External provider credential is required.");

        var normalizedProvider = provider.Trim().ToLowerInvariant();
        var identity = normalizedProvider switch
        {
            "google" => await ValidateGoogleAsync(credential, cancellationToken),
            "facebook" => await ValidateFacebookAsync(credential, cancellationToken),
            _ => throw new InvalidOperationException("Unsupported external provider.")
        };

        var external = await ExternalLogins.SingleOrDefaultAsync(
            x => x.Provider == identity.Provider && x.ProviderUserId == identity.ProviderUserId,
            cancellationToken);

        User user;
        var isNewUser = false;

        if (external is not null)
        {
            user = await Users.SingleOrDefaultAsync(x => x.Id == external.UserId, cancellationToken)
                ?? throw new UnauthorizedAccessException("The linked account no longer exists.");

            if (!user.IsActive)
                throw new UnauthorizedAccessException("Account is not active.");

            external.Email = identity.Email ?? external.Email;
            external.DisplayName = identity.DisplayName ?? external.DisplayName;
            external.LastLoginAt = DateTimeOffset.UtcNow;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(identity.Email))
                throw new InvalidOperationException("The provider did not return a verified email address.");

            var normalizedEmail = identity.Email.Trim().ToLowerInvariant();
            var existingUser = await Users.SingleOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken);

            if (existingUser is not null)
                throw new ExternalAccountLinkRequiredException(
                    "An account with this email already exists. Sign in with your existing account and explicitly link this provider.");

            user = new User
            {
                Id = Guid.NewGuid(),
                Email = normalizedEmail,
                PasswordHash = "EXTERNAL_ONLY$v1$" + Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)),
                IsEmailVerified = identity.EmailVerified,
                IsActive = true
            };

            Users.Add(user);
            ExternalLogins.Add(new ExternalLogin
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Provider = identity.Provider,
                ProviderUserId = identity.ProviderUserId,
                Email = identity.Email,
                DisplayName = identity.DisplayName
            });
            isNewUser = true;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        var tokens = await authService.IssueTokensForExternalLoginAsync(user, identity.Provider, context, cancellationToken);
        return new ExternalAuthResult(tokens.AccessToken, tokens.RefreshToken, tokens.SessionId, isNewUser, identity.Provider);
    }

    private async Task<ExternalIdentity> ValidateGoogleAsync(string idToken, CancellationToken cancellationToken)
    {
        var clientId = configuration["Authentication:Google:ClientId"];
        if (string.IsNullOrWhiteSpace(clientId))
            throw new InvalidOperationException("Google login is not configured.");

        try
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(
                idToken,
                new GoogleJsonWebSignature.ValidationSettings { Audience = new[] { clientId } });

            if (string.IsNullOrWhiteSpace(payload.Subject) ||
                string.IsNullOrWhiteSpace(payload.Email) ||
                payload.EmailVerified != true)
                throw new UnauthorizedAccessException("Google account email could not be verified.");

            return new ExternalIdentity(
                "google",
                payload.Subject,
                payload.Email,
                payload.Name,
                true);
        }
        catch (InvalidJwtException)
        {
            throw new UnauthorizedAccessException("Invalid Google credential.");
        }
    }

    private async Task<ExternalIdentity> ValidateFacebookAsync(string accessToken, CancellationToken cancellationToken)
    {
        var appId = configuration["Authentication:Facebook:AppId"];
        var appSecret = configuration["Authentication:Facebook:AppSecret"];
        if (string.IsNullOrWhiteSpace(appId) || string.IsNullOrWhiteSpace(appSecret))
            throw new InvalidOperationException("Facebook login is not configured.");

        var graphVersion = configuration["Authentication:Facebook:GraphApiVersion"] ?? "v24.0";
        var client = httpClientFactory.CreateClient("facebook-graph");

        var debugUri =
            $"https://graph.facebook.com/{graphVersion}/debug_token" +
            $"?input_token={Uri.EscapeDataString(accessToken)}" +
            $"&access_token={Uri.EscapeDataString(appId + "|" + appSecret)}";

        using var debugResponse = await client.GetAsync(debugUri, cancellationToken);
        if (!debugResponse.IsSuccessStatusCode)
            throw new UnauthorizedAccessException("Invalid Facebook credential.");

        await using var debugStream = await debugResponse.Content.ReadAsStreamAsync(cancellationToken);
        var debug = await JsonSerializer.DeserializeAsync<FacebookDebugResponse>(debugStream, cancellationToken: cancellationToken);
        if (debug?.Data is null ||
            debug.Data.IsValid != true ||
            !string.Equals(debug.Data.AppId, appId, StringComparison.Ordinal) ||
            string.IsNullOrWhiteSpace(debug.Data.UserId))
            throw new UnauthorizedAccessException("Invalid Facebook credential.");

        var meUri =
            $"https://graph.facebook.com/{graphVersion}/me" +
            $"?fields=id,name,email&access_token={Uri.EscapeDataString(accessToken)}";

        using var meResponse = await client.GetAsync(meUri, cancellationToken);
        if (!meResponse.IsSuccessStatusCode)
            throw new UnauthorizedAccessException("Unable to read Facebook account.");

        await using var meStream = await meResponse.Content.ReadAsStreamAsync(cancellationToken);
        var me = await JsonSerializer.DeserializeAsync<FacebookMeResponse>(meStream, cancellationToken: cancellationToken);
        if (me is null || !string.Equals(me.Id, debug.Data.UserId, StringComparison.Ordinal))
            throw new UnauthorizedAccessException("Invalid Facebook account identity.");

        return new ExternalIdentity(
            "facebook",
            me.Id,
            me.Email,
            me.Name,
            !string.IsNullOrWhiteSpace(me.Email));
    }

    private sealed record ExternalIdentity(
        string Provider,
        string ProviderUserId,
        string? Email,
        string? DisplayName,
        bool EmailVerified);

    private sealed class FacebookDebugResponse
    {
        public FacebookDebugData? Data { get; set; }
    }

    private sealed class FacebookDebugData
    {
        [JsonPropertyName("is_valid")]
        public bool IsValid { get; set; }

        [JsonPropertyName("app_id")]
        public string? AppId { get; set; }

        [JsonPropertyName("user_id")]
        public string? UserId { get; set; }
    }

    private sealed class FacebookMeResponse
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }
    }
}

public sealed record ExternalAuthResult(
    string AccessToken,
    string RefreshToken,
    Guid SessionId,
    bool IsNewUser,
    string Provider);

public sealed class ExternalAccountLinkRequiredException(string message) : Exception(message);
