using System.Text;
using System.Threading.RateLimiting;
using MilanSetu.Api.Data;
using MilanSetu.Api.Data.Repositories;
using MilanSetu.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
var isProduction = builder.Environment.IsProduction();
const long MaxRequestBodyBytes = 10L * 1024 * 1024;

builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = MaxRequestBodyBytes);

builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSignalR();
builder.Services.AddSingleton<IRealtimeNotificationService, RealtimeNotificationService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<SecurityAuditService>();
builder.Services.AddScoped<LegalAcceptanceService>();
builder.Services.AddHttpClient("facebook-graph", client => client.Timeout = TimeSpan.FromSeconds(10));
builder.Services.AddScoped<ExternalAuthService>();
builder.Services.AddScoped<ReviewerAuthorizationService>();
builder.Services.AddSingleton<MessageModerationService>();
builder.Services.AddSingleton<VerificationChallengeService>();
builder.Services.AddSingleton<IVerificationCodeSender, VerificationCodeSender>();
builder.Services.AddSingleton<IVerificationDocumentStorage, FileSystemVerificationDocumentStorage>();
builder.Services.AddSingleton<IVerificationDocumentScanner, QuarantineOnlyVerificationDocumentScanner>();
builder.Services.AddSingleton<ModerationAutomationService>();
builder.Services.AddSingleton<RequestMetricsService>();
builder.Services.AddScoped<IUnitOfWork, EfUnitOfWork>();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var databaseProvider = builder.Configuration["Database:Provider"]?.Trim();
if (string.IsNullOrWhiteSpace(databaseProvider))
    databaseProvider = "PostgreSql";

if (!string.IsNullOrWhiteSpace(connectionString))
{
    switch (databaseProvider.ToLowerInvariant())
    {
        case "postgresql":
        case "postgres":
        case "npgsql":
            builder.Services.AddDbContext<MilanSetuDbContext>(options => options.UseNpgsql(connectionString));
            databaseProvider = "PostgreSQL";
            break;

        case "mysql":
        case "mariadb":
            builder.Services.AddDbContext<MilanSetuDbContext>(options =>
                options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 36))));
            databaseProvider = "MySQL";
            break;

        default:
            throw new InvalidOperationException(
                "Unsupported Database:Provider. Supported values are PostgreSQL or MySQL.");
    }
}
else if (isProduction)
{
    throw new InvalidOperationException("Production requires ConnectionStrings:DefaultConnection.");
}

var jwtKey = builder.Configuration["Auth:Jwt:Key"];
var issuer = builder.Configuration["Auth:Jwt:Issuer"];
var audience = builder.Configuration["Auth:Jwt:Audience"];
if (isProduction && (string.IsNullOrWhiteSpace(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < 32))
    throw new InvalidOperationException("Production requires Auth:Jwt:Key with at least 32 UTF-8 bytes.");
if (isProduction && (string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(audience)))
    throw new InvalidOperationException("Production requires Auth:Jwt:Issuer and Auth:Jwt:Audience.");
var verificationHashKey = builder.Configuration["Verification:HashKey"];
if (isProduction && (string.IsNullOrWhiteSpace(verificationHashKey) || Encoding.UTF8.GetByteCount(verificationHashKey) < 32))
    throw new InvalidOperationException("Production requires Verification:HashKey with at least 32 UTF-8 bytes.");

if (!string.IsNullOrWhiteSpace(jwtKey) && Encoding.UTF8.GetByteCount(jwtKey) >= 32)
{
    issuer ??= "MilanSetu";
    audience ??= "MilanSetu.Web";
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
    {
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments(NotificationHub.Route))
                    context.Token = accessToken;
                return Task.CompletedTask;
            }
        };
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });
}
else
{
    builder.Services.AddAuthentication();
}

builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.Headers.RetryAfter = "60";
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsJsonAsync(new { message = "Too many requests. Please try again later." }, cancellationToken);
    };
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var key = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 120,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        });
    });
    options.AddPolicy("auth", context =>
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter($"auth:{ip}", _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        });
    });
    options.AddPolicy("verification", context =>
    {
        var user = context.User.FindFirst("sub")?.Value ?? "anonymous";
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter($"verification:{user}:{ip}", _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(10),
            QueueLimit = 0,
            AutoReplenishment = true
        });
    });
});

var configuredOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
if (isProduction && configuredOrigins.Length == 0)
    throw new InvalidOperationException("Production requires Cors:AllowedOrigins.");
var allowedOrigins = configuredOrigins.Length > 0 ? configuredOrigins : new[] { "http://localhost:5173" };
builder.Services.AddCors(options => options.AddPolicy("Web", policy =>
    policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var bootstrapSuperAdminEmail = builder.Configuration["SuperAdmin:Email"]?.Trim();
var app = builder.Build();

app.UseForwardedHeaders();
app.UseMiddleware<RequestMetricsMiddleware>();
if (isProduction)
{
    app.UseExceptionHandler(exceptionApp => exceptionApp.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { error = "An unexpected error occurred." });
    }));
    app.UseHsts();
}

app.UseHttpsRedirection();
app.Use(async (context, next) =>
{
    if (context.Request.ContentLength is > MaxRequestBodyBytes)
    {
        context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
        await context.Response.WriteAsJsonAsync(new { message = "Request payload is too large." });
        return;
    }

    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    context.Response.Headers["Cross-Origin-Resource-Policy"] = "same-origin";
    context.Response.Headers["Cross-Origin-Opener-Policy"] = "same-origin";
    context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self' https://pagead2.googlesyndication.com https://googleads.g.doubleclick.net; img-src 'self' data: https:; frame-src 'self' https://googleads.g.doubleclick.net https://tpc.googlesyndication.com; connect-src 'self' https:; frame-ancestors 'none'; base-uri 'self'; form-action 'self';";
    await next();
});
app.UseCors("Web");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
if (!string.IsNullOrWhiteSpace(bootstrapSuperAdminEmail) && app.Services.GetService<MilanSetuDbContext>() is not null)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<MilanSetuDbContext>();
    var user = await db.Users.SingleOrDefaultAsync(x => x.Email == bootstrapSuperAdminEmail);
    if (user is not null)
    {
        var assignment = await db.UserRoleAssignments.SingleOrDefaultAsync(x => x.UserId == user.Id);
        if (assignment is null) db.UserRoleAssignments.Add(new UserRoleAssignment { UserId = user.Id, Role = UserRole.SuperAdmin });
        else if (assignment.Role != UserRole.SuperAdmin) { assignment.Role = UserRole.SuperAdmin; assignment.UpdatedAt = DateTimeOffset.UtcNow; }
        await db.SaveChangesAsync();
    }
}

app.MapControllers();
app.MapHub<NotificationHub>(NotificationHub.Route);
app.MapGet("/api/health", () => Results.Ok(new { status = "ok", service = "MilanSetu.Api" }));
app.MapGet("/api/health/database", async (IServiceProvider services, CancellationToken cancellationToken) =>
{
    var db = services.GetService<MilanSetuDbContext>();
    if (db is null)
        return Results.Ok(new { status = "not-configured", database = databaseProvider });

    try
    {
        var canConnect = await db.Database.CanConnectAsync(cancellationToken);
        return canConnect
            ? Results.Ok(new { status = "ok", database = databaseProvider })
            : Results.Json(new { status = "unavailable", database = databaseProvider }, statusCode: 503);
    }
    catch
    {
        return Results.Json(new { status = "unavailable", database = databaseProvider }, statusCode: 503);
    }
});
app.Run();
