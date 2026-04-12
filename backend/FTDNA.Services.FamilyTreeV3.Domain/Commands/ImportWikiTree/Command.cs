namespace FTDNA.Services.FamilyTreeV3.Domain.Commands.ImportWikiTree;

public record ImportWikiTreeCommand(
    string WikiTreeId,
    int AncestorDepth = 5,
    int DescendantDepth = 2,
    string? SessionToken = null);
