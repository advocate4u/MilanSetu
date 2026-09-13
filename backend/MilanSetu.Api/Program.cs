using System.Text;
using System.Threading.RateLimiting;
using MilanSetu.Api.Data;
using MilanSetu.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
var isProduction = builder.Environment.IsProduction();

builder.Services.AddControllers();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ReviewerAuthorizationService>();
builder.Services.AddSingleton<MessageModerationService>();
builder.Services.AddSingleton<VerificationChallengeService>();
builder.Services.AddSingleton<IVerificationCodeSender, VerificationCodeSender>();
builder.Services.AddSingleton<IVerificationDocumentStorage, FileSystemVerificationDocumentStorage>();
builder.Services.AddSingleton<IVerificationDocumentScanner, QuarantineOnlyVerificationDocumentScanner>();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrWhiteSpace(connectionString))
    builder.Services.AddDbContext<MilanSetuDbContext>(options => options.UseNpgsql(connectionString));
else if (isProduction)
    throw new InvalidOperationException("Production requires ConnectionStrings:DefaultConnection.");

var jwtKey = builder.Configuration["Auth:Jwt:Key"];
var issuer = builder.Configuration["Auth:Jwt:Issuer"];
var audience = builder.Configuration["Auth:Jwt:Audience"];
if (isProduction && (string.IsNullOrWhiteSpace(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < 32))
    throw new InvalidOperationException("Production requires Auth:Jwt:Key with at least 32 UTF-8 bytes.");
if (isProduction && (string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(audience)))
    throw new InvalidOperationException("Production requires Auth:Jwt:Issuer and Auth:Jwt:Audience.");

if (!string.IsNullOrWhiteSpace(jwtKey) && Encoding.UTF8.GetByteCount(jwtKey) >= 32)
{
    issuer ??= "MilanSetu";
    audience ??= "MilanSetu.Web";
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
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
    options.AddFixedWindowLimiter("auth", limiter =>
    {
        limiter.PermitLimit = 10;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
    });
    options.AddFixedWindowLimiter("verification", limiter =>
    {
        limiter.PermitLimit = 5;
        limiter.Window = TimeSpan.FromMinutes(10);
        limiter.QueueLimit = 0;
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

var app = builder.Build();

app.UseForwardedHeaders();
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
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    await next();
});
app.UseCors("Web");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/api/health", () => Results.Ok(new { status = "ok", service = "MilanSetu.Api" }));
app.MapGet("/api/health/database", async (IServiceProvider services, CancellationToken cancellationToken) =>
{
    var db = services.GetService<MilanSetuDbContext>();
    if (db is null) return Results.Ok(new { status = "not-configured", database = "postgresql" });
    try
    {
        var canConnect = await db.Database.CanConnectAsync(cancellationToken);
        return canConnect
            ? Results.Ok(new { status = "ok", database = "postgresql" })
            : Results.Json(new { status = "unavailable", database = "postgresql" }, statusCode: 503);
    }
    catch
    {
        return Results.Json(new { status = "unavailable", database = "postgresql" }, statusCode: 503);
    }
});
app.Run();
