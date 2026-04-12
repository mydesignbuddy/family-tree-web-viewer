using FluentResults;
using FTDNA.Services.FamilyTreeV3.Common.Models;
using FTDNA.Services.FamilyTreeV3.Data.Internal.Store;

namespace FTDNA.Services.FamilyTreeV3.Data.Internal.Queries.GetAncestorChain;

public static class GetAncestorChainQueryHandler
{
    public static Result<AncestorChainResponse> Handle(GetAncestorChainQuery query, FamilyTreeStore store)
    {
        var data = store.Get();
        if (data is null)
            return Result.Fail("No data loaded.");

        var personId = Uri.UnescapeDataString(query.PersonId);

        if (!data.Individuals.ContainsKey(personId))
            return Result.Fail($"Person '{personId}' not found. Sample IDs: {string.Join(", ", data.Individuals.Keys.Take(5))}");

        var chain = new List<AncestorChainEntry>();
        var current = data.Individuals[personId];
        int gen = 0;

        while (gen < 10)
        {
            var famId = current.FamilyAsChild;
            var family = famId is not null && data.Families.ContainsKey(famId) ? data.Families[famId] : null;

            chain.Add(new AncestorChainEntry(
                Generation: gen,
                Id: current.Id,
                Name: $"{current.FirstName} {current.LastName}",
                FamilyAsChild: current.FamilyAsChild,
                FamilyFound: family is not null,
                FatherId: family?.HusbandId,
                MotherId: family?.WifeId));

            if (family?.HusbandId is not null && data.Individuals.ContainsKey(family.HusbandId))
                current = data.Individuals[family.HusbandId];
            else
                break;

            gen++;
        }

        return Result.Ok(new AncestorChainResponse(
            TotalIndividuals: data.Individuals.Count,
            TotalFamilies: data.Families.Count,
            AncestorChain: chain));
    }
}
