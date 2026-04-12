namespace FTDNA.Services.FamilyTreeV3.Common.Models;

public record AncestorChainEntry(
    int Generation,
    string Id,
    string Name,
    string? FamilyAsChild,
    bool FamilyFound,
    string? FatherId,
    string? MotherId);

public record AncestorChainResponse(
    int TotalIndividuals,
    int TotalFamilies,
    IReadOnlyList<AncestorChainEntry> AncestorChain);
