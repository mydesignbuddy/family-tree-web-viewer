namespace FTDNA.Services.FamilyTreeV3.Domain.Commands.ActivateTree;

public record ActivateTreeCommand(string TreeId, int AncestorDepth = 5, int DescendantDepth = 2);
