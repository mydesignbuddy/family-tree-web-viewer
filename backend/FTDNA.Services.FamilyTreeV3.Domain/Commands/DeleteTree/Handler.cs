using FluentResults;
using FTDNA.Services.FamilyTreeV3.Common.Models;
using FTDNA.Services.FamilyTreeV3.Data.Internal.Store;

namespace FTDNA.Services.FamilyTreeV3.Domain.Commands.DeleteTree;

public static class DeleteTreeCommandHandler
{
    public static Result<TreeListResponse> Handle(DeleteTreeCommand command, FamilyTreeStore store)
    {
        if (!store.Remove(command.TreeId))
            return Result.Fail("Tree not found.");

        return Result.Ok(new TreeListResponse(store.ListAll(), store.ActiveTreeId));
    }
}
