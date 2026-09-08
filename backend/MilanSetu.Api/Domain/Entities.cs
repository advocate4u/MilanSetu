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
    public string Importance { get; set; } = "Flexible";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Profile Profile { get; set; } = null!;
}
