using System.Text.RegularExpressions;
using FamilyTreeApi.Models;

namespace FamilyTreeApi.Services;

public class GedcomParserService : IGedcomParserService
{
    public FamilyTreeData Parse(Stream stream)
    {
        var result = new FamilyTreeData();
        Individual? currentIndividual = null;
        Family? currentFamily = null;
        string? currentTag = null;    // level-1 event tag (BIRT, DEAT, MARR)
        string? currentRecordType = null; // "INDI" or "FAM"

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
                // Save previous record
                SaveCurrentRecord(result, currentIndividual, currentFamily);
                currentIndividual = null;
                currentFamily = null;
                currentTag = null;
                currentRecordType = null;

                if (tag == "INDI")
                {
                    currentRecordType = "INDI";
                    currentIndividual = new Individual { Id = xref ?? string.Empty };
                    if (result.RootIndividualId == null)
                        result.RootIndividualId = currentIndividual.Id;
                }
                else if (tag == "FAM")
                {
                    currentRecordType = "FAM";
                    currentFamily = new Family { Id = xref ?? string.Empty };
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

        // Save last record
        SaveCurrentRecord(result, currentIndividual, currentFamily);

        return result;
    }

    private static (int level, string? xref, string tag, string? value)? ParseLine(string line)
    {
        // GEDCOM line format: LEVEL [XREF] TAG [VALUE]
        // Examples:
        //   0 @I1@ INDI
        //   1 NAME John /Smith/
        //   2 DATE 1 JAN 1900
        var match = Regex.Match(line, @"^(\d+)\s+(?:(@\S+@)\s+)?(\S+)(?:\s+(.*))?$");
        if (!match.Success) return null;

        int level = int.Parse(match.Groups[1].Value);
        string? xref = match.Groups[2].Success ? match.Groups[2].Value : null;
        string tag = match.Groups[3].Value;
        string? value = match.Groups[4].Success ? match.Groups[4].Value.Trim() : null;

        // Handle level 0 where tag might actually be the record type after xref
        // e.g., "0 @I1@ INDI" → xref=@I1@, tag=INDI
        // vs "0 HEAD" → xref=null, tag=HEAD

        return (level, xref, tag, value);
    }

    private static void ParseName(string? nameValue, Individual individual)
    {
        if (string.IsNullOrWhiteSpace(nameValue)) return;

        // GEDCOM name format: "FirstName /LastName/" or "FirstName"
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

    private static string CleanXref(string? value)
    {
        return value?.Trim() ?? string.Empty;
    }

    private static void SaveCurrentRecord(FamilyTreeData result, Individual? individual, Family? family)
    {
        if (individual != null && !string.IsNullOrEmpty(individual.Id))
            result.Individuals[individual.Id] = individual;
        if (family != null && !string.IsNullOrEmpty(family.Id))
            result.Families[family.Id] = family;
    }
}
