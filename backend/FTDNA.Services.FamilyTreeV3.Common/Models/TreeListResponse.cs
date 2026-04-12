namespace FTDNA.Services.FamilyTreeV3.Common.Models;

public record TreeListResponse(
    IReadOnlyList<StoredTreeSummary> Trees,
    string? ActiveTreeId);
