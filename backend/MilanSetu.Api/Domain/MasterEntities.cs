namespace MilanSetu.Api.Domain;

public abstract class MasterEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}

public class Country : MasterEntity
{
    public string Code { get; set; } = null!;
}

public class State : MasterEntity
{
    public int CountryId { get; set; }
    public Country Country { get; set; } = null!;
}

public class District : MasterEntity
{
    public int StateId { get; set; }
    public State State { get; set; } = null!;
}

public class City : MasterEntity
{
    public int DistrictId { get; set; }
    public District District { get; set; } = null!;
}

public class BloodGroup : MasterEntity { }
public class MaritalStatus : MasterEntity { }
public class MotherTongue : MasterEntity { }
public class EducationLevel : MasterEntity { }
public class EmploymentTypeMaster : MasterEntity { }
public class DietType : MasterEntity { }
public class SmokingStatus : MasterEntity { }
public class DrinkingStatus : MasterEntity { }
