namespace FTDNA.Services.FamilyTreeV3.Domain.Commands.ExpandWikiTree;

public record ExpandWikiTreeCommand(string IndividualId, int Depth = 5, string? SessionToken = null);
