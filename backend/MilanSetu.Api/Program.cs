using System.Text;
using MilanSetu.Api.Data;
using MilanSetu.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddScoped<AuthService>();
builder.Services.AddSingleton<MessageModerationService>();
builder.Services.AddSingleton<VerificationChallengeService>();
builder.Services.AddSingleton<IVerificationCodeSender, VerificationCodeSender>();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrWhiteSpace(connectionString))
{
    builder.Services.AddDbContext<MilanSetuDbContext>(options =>
        options.UseNpgsql(connectionString));
}

var jwtKey = builder.Configuration["Auth:Jwt:Key"];
if (!string.IsNullOrWhiteSpace(jwtKey) && Encoding.UTF8.GetByteCount(jwtKey) >= 32)
{
    var issuer = builder.Configuration["Auth:Jwt:Issuer"] ?? "MilanSetu";
    var audience = builder.Configuration["Auth:Jwt:Audience"] ?? "MilanSetu.Web";
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
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

builder.Services.AddCors(options =>
{
    options.AddPolicy("Web", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

app.UseHttpsRedirection();
app.UseCors("Web");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/api/health", () => Results.Ok(new
{
    status = "ok",
    service = "MilanSetu.Api"
}));

app.MapGet("/api/health/database", async (IServiceProvider services, CancellationToken cancellationToken) =>
{
    var db = services.GetService<MilanSetuDbContext>();
    if (db is null)
    {
        return Results.Ok(new { status = "not-configured", database = "postgresql" });
    }

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
