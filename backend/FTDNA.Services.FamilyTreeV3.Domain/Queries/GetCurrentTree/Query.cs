namespace FTDNA.Services.FamilyTreeV3.Domain.Queries.GetCurrentTree;

public record GetCurrentTreeQuery(int AncestorDepth = 5, int DescendantDepth = 2);
