namespace FTDNA.Services.FamilyTreeV3.Common.Models;

public record IndividualModel(
    string Id,
    string FirstName,
    string LastName,
    string Sex,
    string? BirthDate,
    string? BirthPlace,
    string? DeathDate,
    string? DeathPlace,
    IReadOnlyList<string> FamiliesAsSpouse,
    string? FamilyAsChild);
