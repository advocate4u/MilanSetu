namespace MilanSetu.Api.Domain;

public class Religion { public int Id { get; set; } public string Name { get; set; } = null!; public bool IsActive { get; set; } = true; }
public class Community { public int Id { get; set; } public int ReligionId { get; set; } public string Name { get; set; } = null!; public bool IsActive { get; set; } = true; public Religion Religion { get; set; } = null!; }
public class Caste { public int Id { get; set; } public int? CommunityId { get; set; } public string Name { get; set; } = null!; public bool IsActive { get; set; } = true; public Community? Community { get; set; } }
public class ProfileIdentityPreference { public Guid ProfileId { get; set; } public int? ReligionId { get; set; } public int? CommunityId { get; set; } public int? CasteId { get; set; } public PreferenceImportance Importance { get; set; } = PreferenceImportance.NoPreference; public Profile Profile { get; set; } = null!; public Religion? Religion { get; set; } public Community? Community { get; set; } public Caste? Caste { get; set; } }
