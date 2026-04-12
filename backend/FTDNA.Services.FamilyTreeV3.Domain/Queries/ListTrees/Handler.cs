using FTDNA.Services.FamilyTreeV3.Common.Models;
using FTDNA.Services.FamilyTreeV3.Data.Internal.Store;

namespace FTDNA.Services.FamilyTreeV3.Domain.Queries.ListTrees;

public static class ListTreesQueryHandler
{
    public static TreeListResponse Handle(ListTreesQuery query, FamilyTreeStore store)
    {
        return new TreeListResponse(store.ListAll(), store.ActiveTreeId);
    }
}
