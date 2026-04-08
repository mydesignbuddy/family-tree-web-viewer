namespace FamilyTreeApi.Models;

public class Individual
{
    public string Id { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Sex { get; set; } = "U";
    public string? BirthDate { get; set; }
    public string? BirthPlace { get; set; }
    public string? DeathDate { get; set; }
    public string? DeathPlace { get; set; }
    public List<string> FamiliesAsSpouse { get; set; } = new();
    public string? FamilyAsChild { get; set; }
}
