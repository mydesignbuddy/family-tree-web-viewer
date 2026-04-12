namespace FTDNA.Services.FamilyTreeV3.Common.Models;

public record StoredTreeModel(
    string Id,
    string Name,
    string Source,
    FamilyTreeModel Data,
    Dictionary<string, string>? WikiTreeNameMap = null);

public record StoredTreeSummary(
    string Id,
    string Name,
    string Source,
    int IndividualCount,
    int FamilyCount,
    string? RootPersonName);
