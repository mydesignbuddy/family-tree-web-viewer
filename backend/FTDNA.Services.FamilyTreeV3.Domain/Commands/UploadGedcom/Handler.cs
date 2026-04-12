using System.Text.RegularExpressions;
using FluentResults;
using FTDNA.Services.FamilyTreeV3.Common.Models;
using FTDNA.Services.FamilyTreeV3.Data.Internal.Store;

namespace FTDNA.Services.FamilyTreeV3.Domain.Commands.UploadGedcom;

public static class UploadGedcomCommandHandler
{
    public static Result<FamilyTreeModel> Handle(UploadGedcomCommand command, FamilyTreeStore store)
    {
        try
        {
            var model = ParseGedcom(command.FileStream);

            if (model.Individuals.Count == 0)
                return Result.Fail("No individuals found in the GEDCOM file.");

            var treeName = Path.GetFileNameWithoutExtension(command.FileName);
            store.Add(treeName, "GEDCOM", model);

            // Return a subset from root so the controller doesn't need store access
            var rootId = model.RootIndividualId;
            if (rootId != null)
            {
                var subset = store.GetSubset(rootId, 5, 2);
                if (subset != null) return Result.Ok(subset);
            }

            return Result.Ok(model);
        }
        catch (Exception ex)
        {
            return Result.Fail($"Failed to parse GEDCOM file: {ex.Message}");
        }
    }

    private static FamilyTreeModel ParseGedcom(Stream stream)
    {
        var individuals = new Dictionary<string, IndividualBuilder>();
        var families = new Dictionary<string, FamilyBuilder>();
        string? rootIndividualId = null;

        IndividualBuilder? currentIndividual = null;
        FamilyBuilder? currentFamily = null;
        string? currentTag = null;
        string? currentRecordType = null;

        using var reader = new StreamReader(stream);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            line = line.Trim();
            if (string.IsNullOrEmpty(line)) continue;

            var parsed = ParseLine(line);
            if (parsed == null) continue;

            var (level, xref, tag, value) = parsed.Value;

            if (level == 0)
            {
                SaveCurrentRecord(individuals, families, currentIndividual, currentFamily);
                currentIndividual = null;
                currentFamily = null;
                currentTag = null;
                currentRecordType = null;

                if (tag == "INDI")
                {
                    currentRecordType = "INDI";
                    currentIndividual = new IndividualBuilder { Id = xref ?? string.Empty };
                    rootIndividualId ??= currentIndividual.Id;
                }
                else if (tag == "FAM")
                {
                    currentRecordType = "FAM";
                    currentFamily = new FamilyBuilder { Id = xref ?? string.Empty };
                }
            }
            else if (level == 1)
            {
                currentTag = tag;

                if (currentRecordType == "INDI" && currentIndividual != null)
                {
                    switch (tag)
                    {
                        case "NAME":
                            ParseName(value, currentIndividual);
                            break;
                        case "SEX":
                            currentIndividual.Sex = value ?? "U";
                            break;
                        case "FAMC":
                            currentIndividual.FamilyAsChild = CleanXref(value);
                            break;
                        case "FAMS":
                            if (value != null)
                                currentIndividual.FamiliesAsSpouse.Add(CleanXref(value));
                            break;
                    }
                }
                else if (currentRecordType == "FAM" && currentFamily != null)
                {
                    switch (tag)
                    {
                        case "HUSB":
                            currentFamily.HusbandId = CleanXref(value);
                            break;
                        case "WIFE":
                            currentFamily.WifeId = CleanXref(value);
                            break;
                        case "CHIL":
                            if (value != null)
                                currentFamily.ChildrenIds.Add(CleanXref(value));
                            break;
                    }
                }
            }
            else if (level == 2)
            {
                if (currentRecordType == "INDI" && currentIndividual != null)
                {
                    if (currentTag == "BIRT")
                    {
                        if (tag == "DATE") currentIndividual.BirthDate = value;
                        else if (tag == "PLAC") currentIndividual.BirthPlace = value;
                    }
                    else if (currentTag == "DEAT")
                    {
                        if (tag == "DATE") currentIndividual.DeathDate = value;
                        else if (tag == "PLAC") currentIndividual.DeathPlace = value;
                    }
                }
                else if (currentRecordType == "FAM" && currentFamily != null)
                {
                    if (currentTag == "MARR")
                    {
                        if (tag == "DATE") currentFamily.MarriageDate = value;
                        else if (tag == "PLAC") currentFamily.MarriagePlace = value;
                    }
                }
            }
        }

        SaveCurrentRecord(individuals, families, currentIndividual, currentFamily);

        return new FamilyTreeModel(
            Individuals: individuals.ToDictionary(kv => kv.Key, kv => kv.Value.ToModel()),
            Families: families.ToDictionary(kv => kv.Key, kv => kv.Value.ToModel()),
            RootIndividualId: rootIndividualId);
    }

    private static (int level, string? xref, string tag, string? value)? ParseLine(string line)
    {
        var match = Regex.Match(line, @"^(\d+)\s+(?:(@\S+@)\s+)?(\S+)(?:\s+(.*))?$");
        if (!match.Success) return null;

        int level = int.Parse(match.Groups[1].Value);
        string? xref = match.Groups[2].Success ? match.Groups[2].Value : null;
        string tag = match.Groups[3].Value;
        string? value = match.Groups[4].Success ? match.Groups[4].Value.Trim() : null;

        return (level, xref, tag, value);
    }

    private static void ParseName(string? nameValue, IndividualBuilder individual)
    {
        if (string.IsNullOrWhiteSpace(nameValue)) return;

        var match = Regex.Match(nameValue, @"^(.*?)(?:\s*/([^/]*)/)?$");
        if (match.Success)
        {
            individual.FirstName = match.Groups[1].Value.Trim();
            if (match.Groups[2].Success)
                individual.LastName = match.Groups[2].Value.Trim();
        }
        else
        {
            individual.FirstName = nameValue.Trim();
        }
    }

    private static string CleanXref(string? value) => value?.Trim() ?? string.Empty;

    private static void SaveCurrentRecord(
        Dictionary<string, IndividualBuilder> individuals,
        Dictionary<string, FamilyBuilder> families,
        IndividualBuilder? individual,
        FamilyBuilder? family)
    {
        if (individual != null && !string.IsNullOrEmpty(individual.Id))
            individuals[individual.Id] = individual;
        if (family != null && !string.IsNullOrEmpty(family.Id))
            families[family.Id] = family;
    }

    // Mutable builders used during parsing, converted to immutable records at the end
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
