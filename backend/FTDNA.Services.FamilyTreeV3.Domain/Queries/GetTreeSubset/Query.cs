namespace FTDNA.Services.FamilyTreeV3.Domain.Queries.GetTreeSubset;

public record GetTreeSubsetQuery(string PersonId, int AncestorDepth = 5, int DescendantDepth = 2);
