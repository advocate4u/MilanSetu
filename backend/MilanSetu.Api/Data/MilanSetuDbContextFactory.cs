using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using MilanSetu.Api.Data;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

namespace MilanSetu.Api.Data;

public sealed class MilanSetuDbContextFactory : IDesignTimeDbContextFactory<MilanSetuDbContext>
{
    public MilanSetuDbContext CreateDbContext(string[] args)
    {
        var provider = GetArgument(args, "--provider") ?? Environment.GetEnvironmentVariable("Database__Provider") ?? "PostgreSql";
        var connection = GetArgument(args, "--connection")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING");
        connection ??= provider.Equals("MySQL", StringComparison.OrdinalIgnoreCase)
            ? "Server=localhost;Port=3306;Database=milansetu;User=root;Password=design-time-only;"
            : "Host=localhost;Port=5432;Database=milansetu;Username=postgres;Password=design-time-only;";
        var optionsBuilder = new DbContextOptionsBuilder<MilanSetuDbContext>();
        switch (provider.Trim().ToLowerInvariant())
        {
            case "postgresql": case "postgres": case "npgsql": optionsBuilder.UseNpgsql(connection); break;
            case "mysql": case "mariadb": optionsBuilder.UseMySql(connection, new MySqlServerVersion(new Version(8, 0, 36))); break;
            default: throw new InvalidOperationException("Unsupported Database:Provider. Supported values are PostgreSQL or MySQL.");
        }
        return new MilanSetuDbContext(optionsBuilder.Options);
    }
    private static string? GetArgument(string[] args, string name)
    {
        var prefix = name + "=";
        var value = args.FirstOrDefault(x => x.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        return value?[prefix.Length..].Trim();
    }
}