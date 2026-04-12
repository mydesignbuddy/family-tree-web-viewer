namespace FTDNA.Services.FamilyTreeV3.Common.Models;

public record FamilyTreeModel(
    IReadOnlyDictionary<string, IndividualModel> Individuals,
    IReadOnlyDictionary<string, FamilyModel> Families,
    string? RootIndividualId);
