namespace FTDNA.Services.FamilyTreeV3.Common.Models;

public record FamilyModel(
    string Id,
    string? HusbandId,
    string? WifeId,
    IReadOnlyList<string> ChildrenIds,
    string? MarriageDate,
    string? MarriagePlace);
