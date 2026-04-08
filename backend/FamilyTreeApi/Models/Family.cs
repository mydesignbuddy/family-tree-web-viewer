namespace FamilyTreeApi.Models;

public class Family
{
    public string Id { get; set; } = string.Empty;
    public string? HusbandId { get; set; }
    public string? WifeId { get; set; }
    public List<string> ChildrenIds { get; set; } = new();
    public string? MarriageDate { get; set; }
    public string? MarriagePlace { get; set; }
}
