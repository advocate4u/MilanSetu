using MilanSetu.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace MilanSetu.Api.Data;

public class MilanSetuDbContext(DbContextOptions<MilanSetuDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Profile> Profiles => Set<Profile>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<ProfileLocation> ProfileLocations => Set<ProfileLocation>();
    public DbSet<ProfilePreference> ProfilePreferences => Set<ProfilePreference>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Email).HasMaxLength(320).IsRequired();
            entity.HasIndex(x => x.Email).IsUnique();
            entity.Property(x => x.PhoneNumber).HasMaxLength(32);
            entity.HasIndex(x => x.PhoneNumber).IsUnique();
            entity.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
            entity.HasOne(x => x.Profile)
                .WithOne(x => x.User)
                .HasForeignKey<Profile>(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Profile>(entity =>
        {
            entity.ToTable("profiles");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.UserId).IsUnique();
            entity.Property(x => x.DisplayName).HasMaxLength(120).IsRequired();
            entity.Property(x => x.MaritalStatus).HasMaxLength(40);
            entity.Property(x => x.MotherTongue).HasMaxLength(80);
            entity.Property(x => x.Bio).HasMaxLength(2000);
            entity.Property(x => x.Gender).HasConversion<string>().HasMaxLength(20);
            entity.Property(x => x.AccountType).HasConversion<string>().HasMaxLength(20);
            entity.Property(x => x.Visibility).HasConversion<string>().HasMaxLength(20);
        });

        modelBuilder.Entity<Location>(entity =>
        {
            entity.ToTable("locations");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.CountryCode).HasMaxLength(2).IsRequired();
            entity.Property(x => x.StateName).HasMaxLength(120).IsRequired();
            entity.Property(x => x.DistrictName).HasMaxLength(120).IsRequired();
            entity.Property(x => x.CityName).HasMaxLength(120).IsRequired();
            entity.HasIndex(x => new { x.CountryCode, x.StateName, x.DistrictName, x.CityName }).IsUnique();
        });

        modelBuilder.Entity<ProfileLocation>(entity =>
        {
            entity.ToTable("profile_locations");
            entity.HasKey(x => new { x.ProfileId, x.LocationId });
            entity.HasOne(x => x.Profile).WithMany(x => x.Locations).HasForeignKey(x => x.ProfileId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Location).WithMany(x => x.Profiles).HasForeignKey(x => x.LocationId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProfilePreference>(entity =>
        {
            entity.ToTable("profile_preferences");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.ProfileId).IsUnique();
            entity.Property(x => x.Importance).HasMaxLength(20).IsRequired();
            entity.HasOne(x => x.Profile).WithMany(x => x.Preferences).HasForeignKey(x => x.ProfileId).OnDelete(DeleteBehavior.Cascade);
            entity.ToTable(t => t.HasCheckConstraint("ck_profile_preferences_age_range", "min_age IS NULL OR max_age IS NULL OR min_age <= max_age"));
        });
    }
}
