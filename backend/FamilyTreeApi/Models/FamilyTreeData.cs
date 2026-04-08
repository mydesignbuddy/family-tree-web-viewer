namespace FamilyTreeApi.Models;

public class FamilyTreeData
{
    public Dictionary<string, Individual> Individuals { get; set; } = new();
    public Dictionary<string, Family> Families { get; set; } = new();
    public string? RootIndividualId { get; set; }
}
