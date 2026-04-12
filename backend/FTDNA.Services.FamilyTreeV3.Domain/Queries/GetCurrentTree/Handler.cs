using FluentResults;
using FTDNA.Services.FamilyTreeV3.Common.Models;
using FTDNA.Services.FamilyTreeV3.Data.Internal.Store;

namespace FTDNA.Services.FamilyTreeV3.Domain.Queries.GetCurrentTree;

public static class GetCurrentTreeQueryHandler
{
    public static Result<FamilyTreeModel> Handle(GetCurrentTreeQuery query, FamilyTreeStore store)
    {
        var active = store.GetActive();
        if (active == null)
            return Result.Fail("No data loaded.");

        var rootId = active.Data.RootIndividualId;
        if (rootId != null)
        {
            var subset = store.GetSubset(rootId, query.AncestorDepth, query.DescendantDepth);
            if (subset != null) return Result.Ok(subset);
        }

        return Result.Ok(active.Data);
    }
}
