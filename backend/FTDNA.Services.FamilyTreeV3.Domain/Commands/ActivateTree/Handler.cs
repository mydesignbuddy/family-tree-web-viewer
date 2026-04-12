using FluentResults;
using FTDNA.Services.FamilyTreeV3.Common.Models;
using FTDNA.Services.FamilyTreeV3.Data.Internal.Store;

namespace FTDNA.Services.FamilyTreeV3.Domain.Commands.ActivateTree;

public static class ActivateTreeCommandHandler
{
    public static Result<FamilyTreeModel> Handle(ActivateTreeCommand command, FamilyTreeStore store)
    {
        if (!store.SetActive(command.TreeId))
            return Result.Fail("Tree not found.");

        var active = store.GetActive();
        var rootId = active!.Data.RootIndividualId;
        if (rootId != null)
        {
            var subset = store.GetSubset(rootId, command.AncestorDepth, command.DescendantDepth);
            if (subset != null) return Result.Ok(subset);
        }

        return Result.Ok(active.Data);
    }
}
