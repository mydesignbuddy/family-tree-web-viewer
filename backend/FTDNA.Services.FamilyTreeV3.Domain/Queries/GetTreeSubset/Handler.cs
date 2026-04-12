using FluentResults;
using FTDNA.Services.FamilyTreeV3.Common.Models;
using FTDNA.Services.FamilyTreeV3.Data.Internal.Store;

namespace FTDNA.Services.FamilyTreeV3.Domain.Queries.GetTreeSubset;

public static class GetTreeSubsetQueryHandler
{
    public static Result<FamilyTreeModel> Handle(GetTreeSubsetQuery query, FamilyTreeStore store)
    {
        var subset = store.GetSubset(query.PersonId, query.AncestorDepth, query.DescendantDepth);
        if (subset == null)
            return Result.Fail("Person not found in the active tree.");

        return Result.Ok(subset);
    }
}
