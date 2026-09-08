using MilanSetu.Api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrWhiteSpace(connectionString))
{
    builder.Services.AddDbContext<MilanSetuDbContext>(options =>
        options.UseNpgsql(connectionString));
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("Web", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseHttpsRedirection();
app.UseCors("Web");
app.MapControllers();

app.MapGet("/api/health", () => Results.Ok(new
{
    status = "ok",
    service = "MilanSetu.Api"
}));

app.MapGet("/api/health/database", async (MilanSetuDbContext? db, CancellationToken cancellationToken) =>
{
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
