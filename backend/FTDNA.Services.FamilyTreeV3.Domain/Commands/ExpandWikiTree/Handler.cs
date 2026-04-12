using FluentResults;
using FTDNA.Services.FamilyTreeV3.Common.Models;
using FTDNA.Services.FamilyTreeV3.Data.External.Services;
using FTDNA.Services.FamilyTreeV3.Data.Internal.Store;
using FTDNA.Services.FamilyTreeV3.Domain.Commands.ImportWikiTree;

namespace FTDNA.Services.FamilyTreeV3.Domain.Commands.ExpandWikiTree;

public static class ExpandWikiTreeCommandHandler
{
    public static async Task<Result<FamilyTreeModel>> Handle(
        ExpandWikiTreeCommand command,
        FamilyTreeStore store,
        WikiTreeApiClient wikiTree,
        WikiTreeSessionStore sessionStore,
        CancellationToken ct)
    {
        var wikiTreeName = store.GetWikiTreeName(command.IndividualId);
        if (string.IsNullOrEmpty(wikiTreeName))
            return Result.Fail("This person doesn't have a WikiTree ID. Expansion is only available for WikiTree-sourced trees.");

        var session = sessionStore.GetSession(command.SessionToken);
        var depth = Math.Clamp(command.Depth, 1, 10);

        try
        {
            var ancestors = await wikiTree.GetPeopleWithAncestorsAsync(wikiTreeName, depth, session, ct);

            var profiles = new Dictionary<long, WikiTreeProfile>();
            foreach (var p in ancestors.Values)
                profiles.TryAdd(p.Id, p);

            if (profiles.Count == 0)
            {
                var current = store.Get();
                return current != null ? Result.Ok(current) : Result.Fail("No active tree.");
            }

            var (model, nameMap) = ImportWikiTreeCommandHandler.MapToFamilyTreeModel(profiles, wikiTreeName);
            var merged = store.MergeIntoActive(model, nameMap);

            if (merged == null)
                return Result.Fail("No active tree.");

            return Result.Ok(merged);
        }
        catch (HttpRequestException ex)
        {
            return Result.Fail($"Failed to fetch from WikiTree: {ex.Message}");
        }
    }
}
