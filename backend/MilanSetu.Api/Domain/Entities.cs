namespace MilanSetu.Api.Domain;

public enum AccountType
{
    Individual = 1,
    Family = 2,
    Assisted = 3
}

public enum Gender
{
    Male = 1,
    Female = 2,
    Other = 3
}

public enum ProfileVisibility
{
    Public = 1,
    MembersOnly = 2,
    Hidden = 3
}

public enum PreferenceImportance
{
    NoPreference = 1,
    Flexible = 2,
    Preferred = 3,
    DealBreaker = 4
}

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string? PhoneNumber { get; set; }
    public bool IsEmailVerified { get; set; }
    public bool IsPhoneVerified { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    public Profile? Profile { get; set; }
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}

public class RefreshToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public Guid? ReplacedByTokenId { get; set; }

    public User User { get; set; } = null!;
}

public class Profile
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = null!;
    public DateOnly DateOfBirth { get; set; }
    public Gender Gender { get; set; }
    public AccountType AccountType { get; set; } = AccountType.Individual;
    public string? MaritalStatus { get; set; }
    public string? MotherTongue { get; set; }
    public string? Bio { get; set; }
    public ProfileVisibility Visibility { get; set; } = ProfileVisibility.MembersOnly;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    public User User { get; set; } = null!;
    public ICollection<ProfileLocation> Locations { get; set; } = new List<ProfileLocation>();
    public ICollection<ProfilePreference> Preferences { get; set; } = new List<ProfilePreference>();
    public Education? Education { get; set; }
    public Employment? Employment { get; set; }
    public FamilyDetails? FamilyDetails { get; set; }
    public Lifestyle? Lifestyle { get; set; }
}

public class Location
{
    public int Id { get; set; }
    public string CountryCode { get; set; } = null!;
    public string StateName { get; set; } = null!;
    public string DistrictName { get; set; } = null!;
    public string CityName { get; set; } = null!;

    public ICollection<ProfileLocation> Profiles { get; set; } = new List<ProfileLocation>();
}

public class ProfileLocation
{
    public Guid ProfileId { get; set; }
    public int LocationId { get; set; }
    public bool IsPrimary { get; set; }

    public Profile Profile { get; set; } = null!;
    public Location Location { get; set; } = null!;
}

public class ProfilePreference
{
    public Guid Id { get; set; }
    public Guid ProfileId { get; set; }
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    public bool? RelocationOpen { get; set; }
    public PreferenceImportance Importance { get; set; } = PreferenceImportance.Flexible;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Profile Profile { get; set; } = null!;
}

public class Education
{
    public Guid ProfileId { get; set; }
    public string? HighestQualification { get; set; }
    public string? FieldOfStudy { get; set; }
    public string? Institution { get; set; }
    public Profile Profile { get; set; } = null!;
}

public class Employment
{
    public Guid ProfileId { get; set; }
    public string? Profession { get; set; }
    public string? Industry { get; set; }
    public string? EmploymentType { get; set; }
    public string? WorkLocation { get; set; }
    public Profile Profile { get; set; } = null!;
}

public class FamilyDetails
{
    public Guid ProfileId { get; set; }
    public string? ParentsStatus { get; set; }
    public string? SiblingsSummary { get; set; }
    public string? FamilyLocation { get; set; }
    public string? FamilyStructure { get; set; }
    public Profile Profile { get; set; } = null!;
}

public class Lifestyle
{
    public Guid ProfileId { get; set; }
    public string? FoodPreference { get; set; }
    public string? Smoking { get; set; }
    public string? Alcohol { get; set; }
    public string? Exercise { get; set; }
    public string? Interests { get; set; }
    public string? Travel { get; set; }
    public string? Pets { get; set; }
    public Profile Profile { get; set; } = null!;
}
