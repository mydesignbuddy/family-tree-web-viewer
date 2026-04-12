using FluentResults;
using FTDNA.Services.FamilyTreeV3.Common.Models;
using FTDNA.Services.FamilyTreeV3.Data.External.Services;
using FTDNA.Services.FamilyTreeV3.Data.Internal.Store;

namespace FTDNA.Services.FamilyTreeV3.Domain.Commands.ImportWikiTree;

public static class ImportWikiTreeCommandHandler
{
    public static async Task<Result<FamilyTreeModel>> Handle(
        ImportWikiTreeCommand command,
        WikiTreeApiClient wikiTree,
        WikiTreeSessionStore sessionStore,
        FamilyTreeStore store,
        CancellationToken ct)
    {
        try
        {
            var session = sessionStore.GetSession(command.SessionToken);

            var ancestorDepth = Math.Clamp(command.AncestorDepth, 0, 10);
            var descendantDepth = Math.Clamp(command.DescendantDepth, 0, 10);

            var ancestorsTask = wikiTree.GetPeopleWithAncestorsAsync(command.WikiTreeId, ancestorDepth, session, ct);
            var descendantsTask = wikiTree.GetPeopleWithDescendantsAsync(command.WikiTreeId, descendantDepth, session, ct);

            await Task.WhenAll(ancestorsTask, descendantsTask);

            var ancestors = await ancestorsTask;
            var descendants = await descendantsTask;

            // Merge and deduplicate all profiles by numeric Id
            var allProfiles = new Dictionary<long, WikiTreeProfile>();
            foreach (var p in ancestors.Values.Concat(descendants.Values))
            {
                allProfiles.TryAdd(p.Id, p);
            }

            if (allProfiles.Count == 0)
                return Result.Fail("No profiles found for the given WikiTree ID. The profile may not exist or may be private.");

            var (model, nameMap) = MapToFamilyTreeModel(allProfiles, command.WikiTreeId);

            if (model.Individuals.Count == 0)
                return Result.Fail("No individuals could be loaded from WikiTree.");

            var treeName = command.WikiTreeId;
            var source = session != null ? "WikiTree (Private)" : "WikiTree (Public)";
            store.Add(treeName, source, model, nameMap);
            return Result.Ok(model);
        }
        catch (HttpRequestException ex)
        {
            return Result.Fail($"Failed to connect to WikiTree API: {ex.Message}");
        }
        catch (Exception ex)
        {
            return Result.Fail($"Failed to import from WikiTree: {ex.Message}");
        }
    }

    public static (FamilyTreeModel Model, Dictionary<string, string> NameMap) MapToFamilyTreeModel(
        Dictionary<long, WikiTreeProfile> profiles,
        string rootWikiTreeId)
    {
        var individuals = new Dictionary<string, IndividualBuilder>();
        var families = new Dictionary<string, FamilyBuilder>();
        var nameMap = new Dictionary<string, string>();
        string? rootIndividualId = null;

        // First pass: create all individuals
        foreach (var (id, profile) in profiles)
        {
            var indId = $"WT-{id}";
            individuals[indId] = new IndividualBuilder
            {
                Id = indId,
                FirstName = profile.FirstName ?? string.Empty,
                LastName = profile.LastNameAtBirth ?? profile.LastNameCurrent ?? string.Empty,
                Sex = MapGender(profile.Gender),
                BirthDate = FormatDate(profile.BirthDate),
                BirthPlace = profile.BirthLocation,
                DeathDate = FormatDate(profile.DeathDate),
                DeathPlace = profile.DeathLocation
            };

            // Store WikiTree Name mapping for lazy-loading
            if (!string.IsNullOrEmpty(profile.Name))
                nameMap[indId] = profile.Name;

            // Identify root individual by WikiTree name (e.g. "Tourps-1")
            if (profile.Name != null &&
                profile.Name.Equals(rootWikiTreeId, StringComparison.OrdinalIgnoreCase))
            {
                rootIndividualId = indId;
            }
        }

        // Fallback: use first profile as root if name match failed
        rootIndividualId ??= $"WT-{profiles.Keys.First()}";

        // Second pass: synthesize families from parent-child relationships
        foreach (var (id, profile) in profiles)
        {
            var childId = $"WT-{id}";
            var fatherId = profile.Father.HasValue && profile.Father.Value != 0 ? profile.Father.Value : (long?)null;
            var motherId = profile.Mother.HasValue && profile.Mother.Value != 0 ? profile.Mother.Value : (long?)null;

            if (fatherId == null && motherId == null)
                continue;

            var famKey = $"FAM-{fatherId ?? 0}-{motherId ?? 0}";

            if (!families.TryGetValue(famKey, out var family))
            {
                family = new FamilyBuilder { Id = famKey };

                if (fatherId.HasValue)
                {
                    var fId = $"WT-{fatherId.Value}";
                    family.HusbandId = fId;
                    if (individuals.TryGetValue(fId, out var father))
                        father.FamiliesAsSpouse.Add(famKey);
                }

                if (motherId.HasValue)
                {
                    var mId = $"WT-{motherId.Value}";
                    family.WifeId = mId;
                    if (individuals.TryGetValue(mId, out var mother))
                        mother.FamiliesAsSpouse.Add(famKey);
                }

                families[famKey] = family;
            }

            family.ChildrenIds.Add(childId);
            if (individuals.TryGetValue(childId, out var child))
                child.FamilyAsChild = famKey;
        }

        var model = new FamilyTreeModel(
            Individuals: individuals.ToDictionary(kv => kv.Key, kv => kv.Value.ToModel()),
            Families: families.ToDictionary(kv => kv.Key, kv => kv.Value.ToModel()),
            RootIndividualId: rootIndividualId);
        return (model, nameMap);
    }

    private static string MapGender(string? gender) => gender switch
    {
        "Male" => "M",
        "Female" => "F",
        _ => "U"
    };

    private static string? FormatDate(string? date)
    {
        if (string.IsNullOrWhiteSpace(date) || date == "0000-00-00")
            return null;

        // WikiTree dates are typically "YYYY-MM-DD" — return as-is
        return date;
    }

    private class IndividualBuilder
    {
        public string Id { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Sex { get; set; } = "U";
        public string? BirthDate { get; set; }
        public string? BirthPlace { get; set; }
        public string? DeathDate { get; set; }
        public string? DeathPlace { get; set; }
        public List<string> FamiliesAsSpouse { get; set; } = new();
        public string? FamilyAsChild { get; set; }

        public IndividualModel ToModel() => new(
            Id, FirstName, LastName, Sex,
            BirthDate, BirthPlace, DeathDate, DeathPlace,
            FamiliesAsSpouse.AsReadOnly(), FamilyAsChild);
    }

    private class FamilyBuilder
    {
        public string Id { get; set; } = string.Empty;
        public string? HusbandId { get; set; }
        public string? WifeId { get; set; }
        public List<string> ChildrenIds { get; set; } = new();
        public string? MarriageDate { get; set; }
        public string? MarriagePlace { get; set; }

        public FamilyModel ToModel() => new(
            Id, HusbandId, WifeId,
            ChildrenIds.AsReadOnly(), MarriageDate, MarriagePlace);
    }
}
