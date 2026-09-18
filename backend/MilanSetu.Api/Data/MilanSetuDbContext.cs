using MilanSetu.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace MilanSetu.Api.Data;

public class MilanSetuDbContext(DbContextOptions<MilanSetuDbContext> options) : DbContext(options)
{
    public DbSet<Country> Countries => Set<Country>();
    public DbSet<State> States => Set<State>();
    public DbSet<District> Districts => Set<District>();
    public DbSet<City> Cities => Set<City>();
    public DbSet<BloodGroup> BloodGroups => Set<BloodGroup>();
    public DbSet<MaritalStatus> MaritalStatuses => Set<MaritalStatus>();
    public DbSet<MotherTongue> MotherTongues => Set<MotherTongue>();
    public DbSet<EducationLevel> EducationLevels => Set<EducationLevel>();
    public DbSet<EmploymentTypeMaster> EmploymentTypes => Set<EmploymentTypeMaster>();
    public DbSet<DietType> DietTypes => Set<DietType>();
    public DbSet<SmokingStatus> SmokingStatuses => Set<SmokingStatus>();
    public DbSet<DrinkingStatus> DrinkingStatuses => Set<DrinkingStatus>();
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<ExternalLogin> ExternalLogins => Set<ExternalLogin>();
    public DbSet<Profile> Profiles => Set<Profile>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<ProfileLocation> ProfileLocations => Set<ProfileLocation>();
    public DbSet<ProfilePreference> ProfilePreferences => Set<ProfilePreference>();
    public DbSet<Education> Educations => Set<Education>();
    public DbSet<Employment> Employments => Set<Employment>();
    public DbSet<FamilyDetails> FamilyDetails => Set<FamilyDetails>();
    public DbSet<Lifestyle> Lifestyles => Set<Lifestyle>();
    public DbSet<Religion> Religions => Set<Religion>();
    public DbSet<Community> Communities => Set<Community>();
    public DbSet<Caste> Castes => Set<Caste>();
    public DbSet<ProfileIdentityPreference> ProfileIdentityPreferences => Set<ProfileIdentityPreference>();
    public DbSet<Interest> Interests => Set<Interest>();
    public DbSet<Connection> Connections => Set<Connection>();
    public DbSet<Block> Blocks => Set<Block>();
    public DbSet<Ignore> Ignores => Set<Ignore>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<UserReport> UserReports => Set<UserReport>();
    public DbSet<ModerationCase> ModerationCases => Set<ModerationCase>();
    public DbSet<VerificationRequest> VerificationRequests => Set<VerificationRequest>();
    public DbSet<VerificationChallenge> VerificationChallenges => Set<VerificationChallenge>();
    public DbSet<UserRoleAssignment> UserRoleAssignments => Set<UserRoleAssignment>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<VerificationDocument> VerificationDocuments => Set<VerificationDocument>();
    public DbSet<LegalAcceptance> LegalAcceptances => Set<LegalAcceptance>();
    public DbSet<AdvertisementSettings> AdvertisementSettings => Set<AdvertisementSettings>();
    public DbSet<ContactSharingSettings> ContactSharingSettings => Set<ContactSharingSettings>();
    public DbSet<ContactShareRequest> ContactShareRequests => Set<ContactShareRequest>();
    public DbSet<PlatformSettings> PlatformSettings => Set<PlatformSettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Country>(entity => { entity.ToTable("countries"); entity.HasKey(x => x.Id); entity.Property(x => x.Name).HasMaxLength(120).IsRequired(); entity.Property(x => x.Code).HasMaxLength(2).IsRequired(); entity.HasIndex(x => x.Code).IsUnique(); entity.HasIndex(x => x.Name).IsUnique(); });
        modelBuilder.Entity<State>(entity => { entity.ToTable("states"); entity.HasKey(x => x.Id); entity.Property(x => x.Name).HasMaxLength(120).IsRequired(); entity.HasIndex(x => new { x.CountryId, x.Name }).IsUnique(); entity.HasOne(x => x.Country).WithMany().HasForeignKey(x => x.CountryId).OnDelete(DeleteBehavior.Restrict); });
        modelBuilder.Entity<District>(entity => { entity.ToTable("districts"); entity.HasKey(x => x.Id); entity.Property(x => x.Name).HasMaxLength(120).IsRequired(); entity.HasIndex(x => new { x.StateId, x.Name }).IsUnique(); entity.HasOne(x => x.State).WithMany().HasForeignKey(x => x.StateId).OnDelete(DeleteBehavior.Restrict); });
        modelBuilder.Entity<City>(entity => { entity.ToTable("cities"); entity.HasKey(x => x.Id); entity.Property(x => x.Name).HasMaxLength(160).IsRequired(); entity.HasIndex(x => new { x.DistrictId, x.Name }).IsUnique(); entity.HasOne(x => x.District).WithMany().HasForeignKey(x => x.DistrictId).OnDelete(DeleteBehavior.Restrict); });
        modelBuilder.Entity<BloodGroup>(entity => { entity.ToTable("blood_groups"); entity.HasKey(x => x.Id); entity.Property(x => x.Name).HasMaxLength(20).IsRequired(); entity.HasIndex(x => x.Name).IsUnique(); });
        modelBuilder.Entity<MaritalStatus>(entity => { entity.ToTable("marital_statuses"); entity.HasKey(x => x.Id); entity.Property(x => x.Name).HasMaxLength(60).IsRequired(); entity.HasIndex(x => x.Name).IsUnique(); });
        modelBuilder.Entity<MotherTongue>(entity => { entity.ToTable("mother_tongues"); entity.HasKey(x => x.Id); entity.Property(x => x.Name).HasMaxLength(80).IsRequired(); entity.HasIndex(x => x.Name).IsUnique(); });
        modelBuilder.Entity<EducationLevel>(entity => { entity.ToTable("education_levels"); entity.HasKey(x => x.Id); entity.Property(x => x.Name).HasMaxLength(120).IsRequired(); entity.HasIndex(x => x.Name).IsUnique(); });
        modelBuilder.Entity<EmploymentTypeMaster>(entity => { entity.ToTable("employment_types"); entity.HasKey(x => x.Id); entity.Property(x => x.Name).HasMaxLength(80).IsRequired(); entity.HasIndex(x => x.Name).IsUnique(); });
        modelBuilder.Entity<DietType>(entity => { entity.ToTable("diet_types"); entity.HasKey(x => x.Id); entity.Property(x => x.Name).HasMaxLength(80).IsRequired(); entity.HasIndex(x => x.Name).IsUnique(); });
        modelBuilder.Entity<SmokingStatus>(entity => { entity.ToTable("smoking_statuses"); entity.HasKey(x => x.Id); entity.Property(x => x.Name).HasMaxLength(80).IsRequired(); entity.HasIndex(x => x.Name).IsUnique(); });
        modelBuilder.Entity<DrinkingStatus>(entity => { entity.ToTable("drinking_statuses"); entity.HasKey(x => x.Id); entity.Property(x => x.Name).HasMaxLength(80).IsRequired(); entity.HasIndex(x => x.Name).IsUnique(); });

        modelBuilder.Entity<Country>().HasData(new Country { Id = 1, Name = "India", Code = "IN", IsActive = true, SortOrder = 1 });
        modelBuilder.Entity<BloodGroup>().HasData(
            new BloodGroup { Id = 1, Name = "A+", SortOrder = 1 }, new BloodGroup { Id = 2, Name = "A-", SortOrder = 2 },
            new BloodGroup { Id = 3, Name = "B+", SortOrder = 3 }, new BloodGroup { Id = 4, Name = "B-", SortOrder = 4 },
            new BloodGroup { Id = 5, Name = "AB+", SortOrder = 5 }, new BloodGroup { Id = 6, Name = "AB-", SortOrder = 6 },
            new BloodGroup { Id = 7, Name = "O+", SortOrder = 7 }, new BloodGroup { Id = 8, Name = "O-", SortOrder = 8 }
        );
        modelBuilder.Entity<MaritalStatus>().HasData(
            new MaritalStatus { Id = 1, Name = "Never Married", SortOrder = 1 }, new MaritalStatus { Id = 2, Name = "Divorced", SortOrder = 2 },
            new MaritalStatus { Id = 3, Name = "Widowed", SortOrder = 3 }, new MaritalStatus { Id = 4, Name = "Separated", SortOrder = 4 }
        );
        modelBuilder.Entity<MotherTongue>().HasData(
            new MotherTongue { Id = 1, Name = "Hindi", SortOrder = 1 }, new MotherTongue { Id = 2, Name = "English", SortOrder = 2 },
            new MotherTongue { Id = 3, Name = "Punjabi", SortOrder = 3 }, new MotherTongue { Id = 4, Name = "Bengali", SortOrder = 4 },
            new MotherTongue { Id = 5, Name = "Marathi", SortOrder = 5 }, new MotherTongue { Id = 6, Name = "Gujarati", SortOrder = 6 },
            new MotherTongue { Id = 7, Name = "Tamil", SortOrder = 7 }, new MotherTongue { Id = 8, Name = "Telugu", SortOrder = 8 },
            new MotherTongue { Id = 9, Name = "Kannada", SortOrder = 9 }, new MotherTongue { Id = 10, Name = "Malayalam", SortOrder = 10 },
            new MotherTongue { Id = 11, Name = "Urdu", SortOrder = 11 }
        );
        modelBuilder.Entity<EducationLevel>().HasData(
            new EducationLevel { Id = 1, Name = "High School", SortOrder = 1 }, new EducationLevel { Id = 2, Name = "Diploma", SortOrder = 2 },
            new EducationLevel { Id = 3, Name = "Bachelor's", SortOrder = 3 }, new EducationLevel { Id = 4, Name = "Master's", SortOrder = 4 },
            new EducationLevel { Id = 5, Name = "Doctorate", SortOrder = 5 }, new EducationLevel { Id = 6, Name = "Professional", SortOrder = 6 }
        );
        modelBuilder.Entity<EmploymentTypeMaster>().HasData(
            new EmploymentTypeMaster { Id = 1, Name = "Full Time", SortOrder = 1 }, new EmploymentTypeMaster { Id = 2, Name = "Part Time", SortOrder = 2 },
            new EmploymentTypeMaster { Id = 3, Name = "Self Employed", SortOrder = 3 }, new EmploymentTypeMaster { Id = 4, Name = "Business", SortOrder = 4 },
            new EmploymentTypeMaster { Id = 5, Name = "Government", SortOrder = 5 }, new EmploymentTypeMaster { Id = 6, Name = "Student", SortOrder = 6 },
            new EmploymentTypeMaster { Id = 7, Name = "Not Working", SortOrder = 7 }
        );
        modelBuilder.Entity<DietType>().HasData(
            new DietType { Id = 1, Name = "Vegetarian", SortOrder = 1 }, new DietType { Id = 2, Name = "Non-Vegetarian", SortOrder = 2 },
            new DietType { Id = 3, Name = "Eggetarian", SortOrder = 3 }, new DietType { Id = 4, Name = "Vegan", SortOrder = 4 }
        );
        modelBuilder.Entity<SmokingStatus>().HasData(
            new SmokingStatus { Id = 1, Name = "Never", SortOrder = 1 }, new SmokingStatus { Id = 2, Name = "Occasionally", SortOrder = 2 },
            new SmokingStatus { Id = 3, Name = "Regularly", SortOrder = 3 }
        );
        modelBuilder.Entity<DrinkingStatus>().HasData(
            new DrinkingStatus { Id = 1, Name = "Never", SortOrder = 1 }, new DrinkingStatus { Id = 2, Name = "Occasionally", SortOrder = 2 },
            new DrinkingStatus { Id = 3, Name = "Regularly", SortOrder = 3 }
        );
        modelBuilder.Entity<User>(entity => { entity.ToTable("users"); entity.HasKey(x => x.Id); entity.Property(x => x.Email).HasMaxLength(320).IsRequired(); entity.HasIndex(x => x.Email).IsUnique(); entity.Property(x => x.PhoneNumber).HasMaxLength(32); entity.HasIndex(x => x.PhoneNumber).IsUnique(); entity.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired(); entity.HasOne(x => x.Profile).WithOne(x => x.User).HasForeignKey<Profile>(x => x.UserId).OnDelete(DeleteBehavior.Cascade); });
        modelBuilder.Entity<ExternalLogin>(entity => { entity.ToTable("external_logins"); entity.HasKey(x => x.Id); entity.Property(x => x.Provider).HasMaxLength(32).IsRequired(); entity.Property(x => x.ProviderUserId).HasMaxLength(256).IsRequired(); entity.Property(x => x.Email).HasMaxLength(320); entity.Property(x => x.DisplayName).HasMaxLength(200); entity.HasIndex(x => new { x.Provider, x.ProviderUserId }).IsUnique(); entity.HasIndex(x => new { x.UserId, x.Provider }).IsUnique(); entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade); });
        modelBuilder.Entity<LoginSession>(entity => { entity.ToTable("login_sessions"); entity.HasKey(x => x.Id); entity.Property(x => x.IPAddress).HasMaxLength(64); entity.Property(x => x.Country).HasMaxLength(120); entity.Property(x => x.State).HasMaxLength(120); entity.Property(x => x.City).HasMaxLength(120); entity.Property(x => x.DeviceType).HasMaxLength(40); entity.Property(x => x.DeviceModel).HasMaxLength(120); entity.Property(x => x.DeviceName).HasMaxLength(120); entity.Property(x => x.DeviceId).HasMaxLength(160); entity.Property(x => x.OS).HasMaxLength(40); entity.Property(x => x.OSVersion).HasMaxLength(40); entity.Property(x => x.AppVersion).HasMaxLength(40); entity.Property(x => x.Browser).HasMaxLength(80); entity.Property(x => x.BrowserVersion).HasMaxLength(40); entity.Property(x => x.LoginProvider).HasMaxLength(32).IsRequired(); entity.Property(x => x.LoginStatus).HasMaxLength(24).IsRequired(); entity.HasIndex(x => new { x.UserId, x.LastSeenAt }); entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade); });
        modelBuilder.Entity<SecurityAuditLog>(entity => { entity.ToTable("security_audit_logs"); entity.HasKey(x => x.Id); entity.Property(x => x.EventType).HasMaxLength(64).IsRequired(); entity.Property(x => x.IPAddress).HasMaxLength(64); entity.Property(x => x.DeviceId).HasMaxLength(160); entity.Property(x => x.Provider).HasMaxLength(32); entity.Property(x => x.FailureReason).HasMaxLength(200); entity.Property(x => x.Metadata).HasMaxLength(4000); entity.HasIndex(x => new { x.UserId, x.Timestamp }); entity.HasIndex(x => new { x.SessionId, x.Timestamp }); entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.SetNull); entity.HasOne(x => x.Session).WithMany(x => x.AuditLogs).HasForeignKey(x => x.SessionId).OnDelete(DeleteBehavior.SetNull); });
        modelBuilder.Entity<RefreshToken>(entity => { entity.ToTable("refresh_tokens"); entity.HasKey(x => x.Id); entity.Property(x => x.SessionId); entity.Property(x => x.TokenHash).HasMaxLength(64).IsRequired(); entity.HasIndex(x => x.TokenHash).IsUnique(); entity.HasIndex(x => new { x.UserId, x.ExpiresAt }); entity.HasOne(x => x.User).WithMany(x => x.RefreshTokens).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade); });
        modelBuilder.Entity<Profile>(entity => { entity.ToTable("profiles"); entity.HasKey(x => x.Id); entity.HasIndex(x => x.UserId).IsUnique(); entity.Property(x => x.DisplayName).HasMaxLength(120).IsRequired(); entity.Property(x => x.MaritalStatus).HasMaxLength(40); entity.Property(x => x.MotherTongue).HasMaxLength(80); entity.Property(x => x.Bio).HasMaxLength(2000); entity.Property(x => x.Gender).HasConversion<string>().HasMaxLength(20); entity.Property(x => x.AccountType).HasConversion<string>().HasMaxLength(20); entity.Property(x => x.Visibility).HasConversion<string>().HasMaxLength(20); });
        modelBuilder.Entity<Location>(entity => { entity.ToTable("locations"); entity.HasKey(x => x.Id); entity.Property(x => x.CountryCode).HasMaxLength(2).IsRequired(); entity.Property(x => x.StateName).HasMaxLength(120).IsRequired(); entity.Property(x => x.DistrictName).HasMaxLength(120).IsRequired(); entity.Property(x => x.CityName).HasMaxLength(120).IsRequired(); entity.HasIndex(x => new { x.CountryCode, x.StateName, x.DistrictName, x.CityName }).IsUnique(); });
        modelBuilder.Entity<ProfileLocation>(entity => { entity.ToTable("profile_locations"); entity.HasKey(x => new { x.ProfileId, x.LocationId }); entity.HasOne(x => x.Profile).WithMany(x => x.Locations).HasForeignKey(x => x.ProfileId).OnDelete(DeleteBehavior.Cascade); entity.HasOne(x => x.Location).WithMany(x => x.Profiles).HasForeignKey(x => x.LocationId).OnDelete(DeleteBehavior.Restrict); });
        modelBuilder.Entity<ProfilePreference>(entity => { entity.ToTable("profile_preferences"); entity.HasKey(x => x.Id); entity.HasIndex(x => x.ProfileId).IsUnique(); entity.Property(x => x.Importance).HasConversion<string>().HasMaxLength(20).IsRequired(); entity.HasOne(x => x.Profile).WithMany(x => x.Preferences).HasForeignKey(x => x.ProfileId).OnDelete(DeleteBehavior.Cascade); entity.ToTable(t => t.HasCheckConstraint("ck_profile_preferences_age_range", "min_age IS NULL OR max_age IS NULL OR min_age <= max_age")); });
        modelBuilder.Entity<Education>(entity => { entity.ToTable("educations"); entity.HasKey(x => x.ProfileId); entity.HasOne(x => x.Profile).WithOne(x => x.Education).HasForeignKey<Education>(x => x.ProfileId).OnDelete(DeleteBehavior.Cascade); });
        modelBuilder.Entity<Employment>(entity => { entity.ToTable("employments"); entity.HasKey(x => x.ProfileId); entity.HasOne(x => x.Profile).WithOne(x => x.Employment).HasForeignKey<Employment>(x => x.ProfileId).OnDelete(DeleteBehavior.Cascade); });
        modelBuilder.Entity<FamilyDetails>(entity => { entity.ToTable("family_details"); entity.HasKey(x => x.ProfileId); entity.HasOne(x => x.Profile).WithOne(x => x.FamilyDetails).HasForeignKey<FamilyDetails>(x => x.ProfileId).OnDelete(DeleteBehavior.Cascade); });
        modelBuilder.Entity<Lifestyle>(entity => { entity.ToTable("lifestyles"); entity.HasKey(x => x.ProfileId); entity.HasOne(x => x.Profile).WithOne(x => x.Lifestyle).HasForeignKey<Lifestyle>(x => x.ProfileId).OnDelete(DeleteBehavior.Cascade); });
        modelBuilder.Entity<Religion>(entity => { entity.ToTable("religions"); entity.HasKey(x => x.Id); entity.Property(x => x.Name).HasMaxLength(120).IsRequired(); entity.HasIndex(x => x.Name).IsUnique(); });
        modelBuilder.Entity<Community>(entity => { entity.ToTable("communities"); entity.HasKey(x => x.Id); entity.Property(x => x.Name).HasMaxLength(120).IsRequired(); entity.HasIndex(x => new { x.ReligionId, x.Name }).IsUnique(); entity.HasOne(x => x.Religion).WithMany().HasForeignKey(x => x.ReligionId).OnDelete(DeleteBehavior.Restrict); });
        modelBuilder.Entity<Caste>(entity => { entity.ToTable("castes"); entity.HasKey(x => x.Id); entity.Property(x => x.Name).HasMaxLength(120).IsRequired(); entity.HasIndex(x => new { x.CommunityId, x.Name }).IsUnique(); entity.HasOne(x => x.Community).WithMany().HasForeignKey(x => x.CommunityId).OnDelete(DeleteBehavior.Restrict); });
        modelBuilder.Entity<ProfileIdentityPreference>(entity => { entity.ToTable("profile_identity_preferences"); entity.HasKey(x => x.ProfileId); entity.Property(x => x.Importance).HasConversion<string>().HasMaxLength(20).IsRequired(); entity.HasOne(x => x.Profile).WithOne().HasForeignKey<ProfileIdentityPreference>(x => x.ProfileId).OnDelete(DeleteBehavior.Cascade); entity.HasOne(x => x.Religion).WithMany().HasForeignKey(x => x.ReligionId).OnDelete(DeleteBehavior.Restrict); entity.HasOne(x => x.Community).WithMany().HasForeignKey(x => x.CommunityId).OnDelete(DeleteBehavior.Restrict); entity.HasOne(x => x.Caste).WithMany().HasForeignKey(x => x.CasteId).OnDelete(DeleteBehavior.Restrict); });
        modelBuilder.Entity<Interest>(entity => { entity.ToTable("interests"); entity.HasKey(x => x.Id); entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired(); entity.HasIndex(x => new { x.SenderUserId, x.ReceiverUserId }).IsUnique(); entity.HasIndex(x => new { x.ReceiverUserId, x.Status }); entity.HasOne(x => x.Sender).WithMany().HasForeignKey(x => x.SenderUserId).OnDelete(DeleteBehavior.Cascade); entity.HasOne(x => x.Receiver).WithMany().HasForeignKey(x => x.ReceiverUserId).OnDelete(DeleteBehavior.Cascade); });
        modelBuilder.Entity<Connection>(entity => { entity.ToTable("connections"); entity.HasKey(x => x.Id); entity.HasIndex(x => new { x.UserAId, x.UserBId }).IsUnique(); entity.HasOne(x => x.UserA).WithMany().HasForeignKey(x => x.UserAId).OnDelete(DeleteBehavior.Cascade); entity.HasOne(x => x.UserB).WithMany().HasForeignKey(x => x.UserBId).OnDelete(DeleteBehavior.Cascade); });
        modelBuilder.Entity<Block>(entity => { entity.ToTable("blocks"); entity.HasKey(x => x.Id); entity.Property(x => x.BlockerUserId).IsRequired(); entity.HasIndex(x => new { x.BlockerUserId, x.BlockedUserId }).IsUnique(); entity.HasOne(x => x.Blocker).WithMany().HasForeignKey(x => x.BlockerUserId).OnDelete(DeleteBehavior.Cascade); entity.HasOne(x => x.Blocked).WithMany().HasForeignKey(x => x.BlockedUserId).OnDelete(DeleteBehavior.Cascade); });
        modelBuilder.Entity<Ignore>(entity => { entity.ToTable("ignores"); entity.HasKey(x => x.Id); entity.HasIndex(x => new { x.IgnorerUserId, x.IgnoredUserId }).IsUnique(); entity.HasIndex(x => new { x.IgnoredUserId, x.CreatedAt }); entity.HasOne(x => x.Ignorer).WithMany().HasForeignKey(x => x.IgnorerUserId).OnDelete(DeleteBehavior.Cascade); entity.HasOne(x => x.Ignored).WithMany().HasForeignKey(x => x.IgnoredUserId).OnDelete(DeleteBehavior.Cascade); });
        modelBuilder.Entity<Conversation>(entity => { entity.ToTable("conversations"); entity.HasKey(x => x.Id); entity.HasIndex(x => new { x.UserAId, x.UserBId }).IsUnique(); entity.HasOne(x => x.UserA).WithMany().HasForeignKey(x => x.UserAId).OnDelete(DeleteBehavior.Cascade); entity.HasOne(x => x.UserB).WithMany().HasForeignKey(x => x.UserBId).OnDelete(DeleteBehavior.Cascade); });
        modelBuilder.Entity<Message>(entity => { entity.ToTable("messages"); entity.HasKey(x => x.Id); entity.Property(x => x.Body).HasMaxLength(4000).IsRequired(); entity.HasIndex(x => new { x.ConversationId, x.CreatedAt }); entity.HasOne(x => x.Conversation).WithMany(x => x.Messages).HasForeignKey(x => x.ConversationId).OnDelete(DeleteBehavior.Cascade); entity.HasOne(x => x.Sender).WithMany().HasForeignKey(x => x.SenderUserId).OnDelete(DeleteBehavior.Restrict); });
        modelBuilder.Entity<Notification>(entity => { entity.ToTable("notifications"); entity.HasKey(x => x.Id); entity.Property(x => x.Type).HasConversion<string>().HasMaxLength(40).IsRequired(); entity.Property(x => x.Title).HasMaxLength(160).IsRequired(); entity.Property(x => x.Body).HasMaxLength(1000).IsRequired(); entity.HasIndex(x => new { x.UserId, x.ReadAt, x.CreatedAt }); entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade); });
        modelBuilder.Entity<UserReport>(entity => { entity.ToTable("user_reports"); entity.HasKey(x => x.Id); entity.Property(x => x.Reason).HasConversion<string>().HasMaxLength(40).IsRequired(); entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired(); entity.Property(x => x.Details).HasMaxLength(2000); entity.HasIndex(x => new { x.ReportedUserId, x.Status, x.CreatedAt }); entity.HasIndex(x => new { x.ReporterUserId, x.CreatedAt }); entity.HasOne(x => x.Reporter).WithMany().HasForeignKey(x => x.ReporterUserId).OnDelete(DeleteBehavior.Restrict); entity.HasOne(x => x.ReportedUser).WithMany().HasForeignKey(x => x.ReportedUserId).OnDelete(DeleteBehavior.Cascade); });
        modelBuilder.Entity<ModerationCase>(entity => { entity.ToTable("moderation_cases"); entity.HasKey(x => x.Id); entity.Property(x => x.Severity).HasConversion<string>().HasMaxLength(20).IsRequired(); entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired(); entity.Property(x => x.Action).HasConversion<string>().HasMaxLength(30).IsRequired(); entity.HasIndex(x => new { x.TargetUserId, x.Status, x.CreatedAt }); entity.HasIndex(x => x.ReportId).IsUnique(); entity.HasOne(x => x.Report).WithMany().HasForeignKey(x => x.ReportId).OnDelete(DeleteBehavior.Cascade); entity.HasOne(x => x.TargetUser).WithMany().HasForeignKey(x => x.TargetUserId).OnDelete(DeleteBehavior.Restrict); });
        modelBuilder.Entity<VerificationRequest>(entity => { entity.ToTable("verification_requests"); entity.HasKey(x => x.Id); entity.Property(x => x.Type).HasConversion<string>().HasMaxLength(30).IsRequired(); entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired(); entity.Property(x => x.ReviewerNotes).HasMaxLength(2000); entity.HasIndex(x => new { x.UserId, x.Type, x.RequestedAt }); entity.HasIndex(x => new { x.Type, x.Status, x.RequestedAt }); entity.HasIndex(x => new { x.ClaimedByUserId, x.ClaimedAt }); entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade); });
        modelBuilder.Entity<VerificationChallenge>(entity => { entity.ToTable("verification_challenges"); entity.HasKey(x => x.Id); entity.Property(x => x.Type).HasConversion<string>().HasMaxLength(30).IsRequired(); entity.Property(x => x.Destination).HasMaxLength(320).IsRequired(); entity.Property(x => x.CodeHash).HasMaxLength(128).IsRequired(); entity.HasIndex(x => new { x.UserId, x.Type, x.CreatedAt }); entity.HasIndex(x => new { x.VerificationRequestId, x.ConsumedAt }); entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade); entity.HasOne(x => x.VerificationRequest).WithMany().HasForeignKey(x => x.VerificationRequestId).OnDelete(DeleteBehavior.Cascade); });
        modelBuilder.Entity<UserRoleAssignment>(entity => { entity.ToTable("user_role_assignments"); entity.HasKey(x => x.UserId); entity.Property(x => x.Role).HasConversion<string>().HasMaxLength(20).IsRequired(); entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade); });
        modelBuilder.Entity<AuditLog>(entity => { entity.ToTable("audit_logs"); entity.HasKey(x => x.Id); entity.Property(x => x.Action).HasMaxLength(120).IsRequired(); entity.Property(x => x.ResourceType).HasMaxLength(80).IsRequired(); entity.Property(x => x.Metadata).HasMaxLength(4000); entity.HasIndex(x => new { x.ResourceType, x.ResourceId, x.CreatedAt }); entity.HasIndex(x => new { x.ActorUserId, x.CreatedAt }); entity.HasOne(x => x.ActorUser).WithMany().HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.SetNull); });
        modelBuilder.Entity<AdvertisementSettings>(entity => { entity.ToTable("advertisement_settings"); entity.HasKey(x => x.Id); entity.Property(x => x.Mode).HasConversion<string>().HasMaxLength(20).IsRequired(); entity.Property(x => x.Label).HasMaxLength(80).IsRequired(); entity.Property(x => x.Text).HasMaxLength(500).IsRequired(); entity.Property(x => x.TargetUrl).HasMaxLength(1000); entity.Property(x => x.ImageUrl).HasMaxLength(1000); entity.Property(x => x.AdSenseClient).HasMaxLength(200); entity.Property(x => x.AdSenseSlot).HasMaxLength(100); entity.HasIndex(x => x.Id).IsUnique(); });
        modelBuilder.Entity<PlatformSettings>(entity => { entity.ToTable("platform_settings"); entity.HasKey(x => x.Id); entity.Property(x => x.Id).ValueGeneratedNever(); entity.Property(x => x.MaxProfilePhotos).IsRequired(); entity.HasIndex(x => x.Id).IsUnique(); });
        modelBuilder.Entity<ContactSharingSettings>(entity => { entity.ToTable("contact_sharing_settings"); entity.HasKey(x => x.Id); entity.Property(x => x.Id).ValueGeneratedNever(); entity.HasIndex(x => x.Id).IsUnique(); });
        modelBuilder.Entity<ContactShareRequest>(entity => { entity.ToTable("contact_share_requests"); entity.HasKey(x => x.Id); entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired(); entity.HasIndex(x => new { x.ConnectionId, x.RequesterUserId, x.RecipientUserId }).IsUnique(); entity.HasIndex(x => new { x.RecipientUserId, x.Status, x.CreatedAt }); entity.HasIndex(x => new { x.RequesterUserId, x.Status, x.CreatedAt }); entity.HasOne(x => x.Requester).WithMany().HasForeignKey(x => x.RequesterUserId).OnDelete(DeleteBehavior.Cascade); entity.HasOne(x => x.Recipient).WithMany().HasForeignKey(x => x.RecipientUserId).OnDelete(DeleteBehavior.Cascade); entity.HasOne<Connection>().WithMany().HasForeignKey(x => x.ConnectionId).OnDelete(DeleteBehavior.Cascade); });
        modelBuilder.Entity<LegalAcceptance>(entity => { entity.ToTable("legal_acceptances"); entity.HasKey(x => x.Id); entity.Property(x => x.AcceptanceType).HasConversion<string>().HasMaxLength(50).IsRequired(); entity.Property(x => x.Version).HasMaxLength(40).IsRequired(); entity.Property(x => x.IPAddress).HasMaxLength(64); entity.Property(x => x.UserAgent).HasMaxLength(1000); entity.HasIndex(x => new { x.UserId, x.AcceptanceType, x.Version }).IsUnique(); entity.HasIndex(x => new { x.UserId, x.AcceptedAt }); entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade); });
        modelBuilder.Entity<VerificationDocument>(entity => { entity.ToTable("verification_documents"); entity.HasKey(x => x.Id); entity.Property(x => x.OriginalFileName).HasMaxLength(180).IsRequired(); entity.Property(x => x.StorageKey).HasMaxLength(500).IsRequired(); entity.Property(x => x.ContentType).HasMaxLength(120).IsRequired(); entity.Property(x => x.Sha256).HasMaxLength(64).IsRequired(); entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired(); entity.Property(x => x.ScannerVerdict).HasMaxLength(200); entity.HasIndex(x => new { x.VerificationRequestId, x.UploadedAt }); entity.HasIndex(x => new { x.UserId, x.Status, x.UploadedAt }); entity.HasOne(x => x.VerificationRequest).WithMany().HasForeignKey(x => x.VerificationRequestId).OnDelete(DeleteBehavior.Cascade); entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade); });
    }
}
