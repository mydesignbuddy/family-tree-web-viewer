using FluentResults;
using FTDNA.Services.FamilyTreeV3.Data.Internal.Store;

namespace FTDNA.Services.FamilyTreeV3.Domain.Commands.ClearFamilyTree;

public static class ClearFamilyTreeCommandHandler
{
    public static Result Handle(ClearFamilyTreeCommand command, FamilyTreeStore store)
    {
        store.Clear();
        return Result.Ok();
    }
}
