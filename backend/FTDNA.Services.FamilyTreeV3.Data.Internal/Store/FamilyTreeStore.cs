using System.Collections.Concurrent;
using FTDNA.Services.FamilyTreeV3.Common.Models;

namespace FTDNA.Services.FamilyTreeV3.Data.Internal.Store;

public class FamilyTreeStore
{
    private readonly ConcurrentDictionary<string, StoredTreeModel> _trees = new();
    private string? _activeTreeId;

    public string Add(string name, string source, FamilyTreeModel data, Dictionary<string, string>? wikiTreeNameMap = null)
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var stored = new StoredTreeModel(id, name, source, data, wikiTreeNameMap);
        _trees[id] = stored;
        _activeTreeId = id;
        return id;
    }

    public bool SetActive(string treeId)
    {
        if (!_trees.ContainsKey(treeId)) return false;
        _activeTreeId = treeId;
        return true;
    }

    public StoredTreeModel? GetActive()
    {
        if (_activeTreeId is null) return null;
        _trees.TryGetValue(_activeTreeId, out var tree);
        return tree;
    }

    public FamilyTreeModel? Get() => GetActive()?.Data;

    public string? ActiveTreeId => _activeTreeId;

    public IReadOnlyList<StoredTreeSummary> ListAll()
    {
        return _trees.Values.Select(t =>
        {
            string? rootName = null;
            if (t.Data.RootIndividualId is not null &&
                t.Data.Individuals.TryGetValue(t.Data.RootIndividualId, out var root))
            {
                rootName = $"{root.FirstName} {root.LastName}".Trim();
            }
            return new StoredTreeSummary(
                t.Id, t.Name, t.Source,
                t.Data.Individuals.Count,
                t.Data.Families.Count,
                rootName);
        }).ToList();
    }

    public bool Remove(string treeId)
    {
        var removed = _trees.TryRemove(treeId, out _);
        if (removed && _activeTreeId == treeId)
        {
            _activeTreeId = _trees.Keys.FirstOrDefault();
        }
        return removed;
    }

    public void Clear()
    {
        _trees.Clear();
        _activeTreeId = null;
    }

    public FamilyTreeModel? MergeIntoActive(FamilyTreeModel newData, Dictionary<string, string>? newNameMap = null)
    {
        var active = GetActive();
        if (active == null) return null;

        var mergedIndividuals = new Dictionary<string, IndividualModel>(active.Data.Individuals);
        foreach (var kvp in newData.Individuals)
            mergedIndividuals.TryAdd(kvp.Key, kvp.Value);

        var mergedFamilies = new Dictionary<string, FamilyModel>(active.Data.Families);
        foreach (var kvp in newData.Families)
            mergedFamilies.TryAdd(kvp.Key, kvp.Value);

        var merged = new FamilyTreeModel(mergedIndividuals, mergedFamilies, active.Data.RootIndividualId);

        var mergedNameMap = active.WikiTreeNameMap != null
            ? new Dictionary<string, string>(active.WikiTreeNameMap)
            : new Dictionary<string, string>();
        if (newNameMap != null)
        {
            foreach (var kvp in newNameMap)
                mergedNameMap.TryAdd(kvp.Key, kvp.Value);
        }

        _trees[active.Id] = active with { Data = merged, WikiTreeNameMap = mergedNameMap };
        return merged;
    }

    public string? GetWikiTreeName(string individualId)
    {
        var active = GetActive();
        if (active?.WikiTreeNameMap != null && active.WikiTreeNameMap.TryGetValue(individualId, out var name))
            return name;
        return null;
    }

    /// <summary>
    /// Extracts a subset of the active tree: ancestors up to ancestorDepth and descendants
    /// down to descendantDepth from the given person.
    /// </summary>
    public FamilyTreeModel? GetSubset(string personId, int ancestorDepth = 5, int descendantDepth = 2)
    {
        var active = GetActive();
        if (active == null) return null;

        var fullData = active.Data;
        if (!fullData.Individuals.ContainsKey(personId))
            return null;

        var includedIndividuals = new HashSet<string>();
        var includedFamilies = new HashSet<string>();

        // Walk ancestors
        CollectAncestors(fullData, personId, ancestorDepth, includedIndividuals, includedFamilies);
        // Walk descendants
        CollectDescendants(fullData, personId, descendantDepth, includedIndividuals, includedFamilies);

        var subsetIndividuals = fullData.Individuals
            .Where(kv => includedIndividuals.Contains(kv.Key))
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        var subsetFamilies = fullData.Families
            .Where(kv => includedFamilies.Contains(kv.Key))
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        return new FamilyTreeModel(subsetIndividuals, subsetFamilies, personId);
    }

    private static void CollectAncestors(FamilyTreeModel data, string personId, int depth,
        HashSet<string> individuals, HashSet<string> families)
    {
        if (depth < 0 || !data.Individuals.ContainsKey(personId)) return;
        individuals.Add(personId);

        var person = data.Individuals[personId];
        if (person.FamilyAsChild is null) return;

        var family = data.Families.GetValueOrDefault(person.FamilyAsChild);
        if (family == null) return;

        families.Add(family.Id);

        if (family.HusbandId is not null)
        {
            individuals.Add(family.HusbandId);
            CollectAncestors(data, family.HusbandId, depth - 1, individuals, families);
        }
        if (family.WifeId is not null)
        {
            individuals.Add(family.WifeId);
            CollectAncestors(data, family.WifeId, depth - 1, individuals, families);
        }
    }

    private static void CollectDescendants(FamilyTreeModel data, string personId, int depth,
        HashSet<string> individuals, HashSet<string> families)
    {
        if (depth < 0 || !data.Individuals.ContainsKey(personId)) return;
        individuals.Add(personId);

        var person = data.Individuals[personId];
        foreach (var famId in person.FamiliesAsSpouse)
        {
            var family = data.Families.GetValueOrDefault(famId);
            if (family == null) continue;

            families.Add(family.Id);

            // Include spouse
            if (family.HusbandId is not null) individuals.Add(family.HusbandId);
            if (family.WifeId is not null) individuals.Add(family.WifeId);

            // Recurse into children
            foreach (var childId in family.ChildrenIds)
            {
                CollectDescendants(data, childId, depth - 1, individuals, families);
            }
        }
    }

    // Legacy compat: Set replaces active or adds new
    public void Set(FamilyTreeModel data)
    {
        Add("Imported Tree", "unknown", data);
    }
}
