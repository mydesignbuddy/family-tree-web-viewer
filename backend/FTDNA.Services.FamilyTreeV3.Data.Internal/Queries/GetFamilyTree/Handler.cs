using FluentResults;
using FTDNA.Services.FamilyTreeV3.Common.Models;
using FTDNA.Services.FamilyTreeV3.Data.Internal.Store;

namespace FTDNA.Services.FamilyTreeV3.Data.Internal.Queries.GetFamilyTree;

public static class GetFamilyTreeQueryHandler
{
    public static Result<FamilyTreeModel> Handle(GetFamilyTreeQuery query, FamilyTreeStore store)
    {
        var data = store.Get();
        return data is not null
            ? Result.Ok(data)
            : Result.Fail("No GEDCOM data loaded.");
    }
}
