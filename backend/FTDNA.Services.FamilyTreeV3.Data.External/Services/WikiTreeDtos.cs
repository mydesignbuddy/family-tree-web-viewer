using System.Text.Json.Serialization;

namespace FTDNA.Services.FamilyTreeV3.Data.External.Services;

public class WikiTreeGetPeopleResponse
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("people")]
    public Dictionary<string, WikiTreeProfile>? People { get; set; }
}

public class WikiTreeProfile
{
    [JsonPropertyName("Id")]
    public long Id { get; set; }

    [JsonPropertyName("Name")]
    public string? Name { get; set; }

    [JsonPropertyName("FirstName")]
    public string? FirstName { get; set; }

    [JsonPropertyName("MiddleName")]
    public string? MiddleName { get; set; }

    [JsonPropertyName("LastNameAtBirth")]
    public string? LastNameAtBirth { get; set; }

    [JsonPropertyName("LastNameCurrent")]
    public string? LastNameCurrent { get; set; }

    [JsonPropertyName("Gender")]
    public string? Gender { get; set; }

    [JsonPropertyName("BirthDate")]
    public string? BirthDate { get; set; }

    [JsonPropertyName("BirthLocation")]
    public string? BirthLocation { get; set; }

    [JsonPropertyName("DeathDate")]
    public string? DeathDate { get; set; }

    [JsonPropertyName("DeathLocation")]
    public string? DeathLocation { get; set; }

    [JsonPropertyName("Father")]
    public long? Father { get; set; }

    [JsonPropertyName("Mother")]
    public long? Mother { get; set; }

    [JsonPropertyName("IsLiving")]
    public int? IsLiving { get; set; }

    [JsonPropertyName("Privacy")]
    public int? Privacy { get; set; }
}
