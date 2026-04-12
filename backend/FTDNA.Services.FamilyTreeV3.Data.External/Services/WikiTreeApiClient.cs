using System.Net;
using System.Text.Json;

namespace FTDNA.Services.FamilyTreeV3.Data.External.Services;

public class WikiTreeApiClient
{
    private const string AppId = "FamilyTreeWebViewer";
    private const string ApiUrl = "https://api.wikitree.com/api.php";
    private readonly HttpClient _httpClient;

    public WikiTreeApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("FamilyTreeWebViewer/1.0 (compatible; +https://github.com/family-tree-web-viewer)");
        _httpClient.DefaultRequestHeaders.Accept.ParseAdd("application/json");
    }

    public async Task<Dictionary<string, WikiTreeProfile>> GetPeopleWithAncestorsAsync(
        string wikiTreeId, int depth, WikiTreeSession? session = null, CancellationToken ct = default)
    {
        var fields = "Id,Name,FirstName,MiddleName,LastNameAtBirth,LastNameCurrent,Gender,BirthDate,BirthLocation,DeathDate,DeathLocation,Father,Mother,IsLiving,Privacy";

        var formData = new Dictionary<string, string>
        {
            ["action"] = "getPeople",
            ["keys"] = wikiTreeId,
            ["ancestors"] = depth.ToString(),
            ["fields"] = fields,
            ["appId"] = AppId
        };

        return await PostGetPeopleAsync(formData, session, ct);
    }

    public async Task<Dictionary<string, WikiTreeProfile>> GetPeopleWithDescendantsAsync(
        string wikiTreeId, int depth, WikiTreeSession? session = null, CancellationToken ct = default)
    {
        var fields = "Id,Name,FirstName,MiddleName,LastNameAtBirth,LastNameCurrent,Gender,BirthDate,BirthLocation,DeathDate,DeathLocation,Father,Mother,IsLiving,Privacy";

        var formData = new Dictionary<string, string>
        {
            ["action"] = "getPeople",
            ["keys"] = wikiTreeId,
            ["descendants"] = depth.ToString(),
            ["fields"] = fields,
            ["appId"] = AppId
        };

        return await PostGetPeopleAsync(formData, session, ct);
    }

    public async Task<WikiTreeLoginResult> ConfirmAuthCodeAsync(string authCode, CancellationToken ct = default)
    {
        var formData = new Dictionary<string, string>
        {
            ["action"] = "clientLogin",
            ["authcode"] = authCode,
            ["appId"] = AppId
        };

        using var content = new FormUrlEncodedContent(formData);
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(ApiUrl)) { Content = content };

        var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        // Capture Set-Cookie headers from WikiTree response
        var cookies = new List<string>();
        if (response.Headers.TryGetValues("Set-Cookie", out var cookieValues))
        {
            cookies.AddRange(cookieValues);
        }

        var json = await response.Content.ReadAsStringAsync(ct);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Response format: {"clientLogin":{"result":"Success","userid":12345,"username":"Name-1"}}
        if (!root.TryGetProperty("clientLogin", out var loginObj) ||
            !loginObj.TryGetProperty("result", out var resultProp) ||
            resultProp.GetString() != "Success")
        {
            return new WikiTreeLoginResult(false, null, null, cookies);
        }

        var userId = loginObj.TryGetProperty("userid", out var uid) ? uid.ToString() : null;
        var userName = loginObj.TryGetProperty("username", out var uname) ? uname.GetString() : null;

        return new WikiTreeLoginResult(true, userId, userName, cookies);
    }

    private const int PageSize = 1000;

    private async Task<Dictionary<string, WikiTreeProfile>> PostGetPeopleAsync(
        Dictionary<string, string> formData, WikiTreeSession? session, CancellationToken ct)
    {
        var allPeople = new Dictionary<string, WikiTreeProfile>();
        var start = 0;

        while (true)
        {
            var pageData = new Dictionary<string, string>(formData)
            {
                ["start"] = start.ToString(),
                ["limit"] = PageSize.ToString()
            };

            using var content = new FormUrlEncodedContent(pageData);
            using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(ApiUrl)) { Content = content };

            if (session?.Cookies is { Count: > 0 })
            {
                request.Headers.Add("Cookie", string.Join("; ", session.Cookies.Select(ExtractCookieNameValue)));
            }

            var response = await _httpClient.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            var result = JsonSerializer.Deserialize<List<WikiTreeGetPeopleResponse>>(json);
            var page = result?.FirstOrDefault();

            if (page?.People == null || page.People.Count == 0)
                break;

            foreach (var kvp in page.People)
            {
                allPeople.TryAdd(kvp.Key, kvp.Value);
            }

            // If status indicates the limit was reached, fetch next page
            var hitLimit = page.Status != null &&
                           page.Status.Contains("Maximum number of profiles", StringComparison.OrdinalIgnoreCase);

            if (!hitLimit || page.People.Count < PageSize)
                break;

            start += page.People.Count;
        }

        return allPeople;
    }

    private static string ExtractCookieNameValue(string setCookieHeader)
    {
        // Set-Cookie headers include attributes like Path, Expires, etc.
        // We only need the name=value portion (before the first semicolon)
        var semicolonIndex = setCookieHeader.IndexOf(';');
        return semicolonIndex >= 0 ? setCookieHeader[..semicolonIndex] : setCookieHeader;
    }
}

public record WikiTreeLoginResult(
    bool Success,
    string? UserId,
    string? UserName,
    List<string> Cookies);
