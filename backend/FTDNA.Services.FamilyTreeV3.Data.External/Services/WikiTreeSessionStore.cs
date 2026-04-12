using System.Collections.Concurrent;

namespace FTDNA.Services.FamilyTreeV3.Data.External.Services;

public class WikiTreeSessionStore
{
    private readonly ConcurrentDictionary<string, WikiTreeSession> _sessions = new();

    public string CreateSession(string userId, string userName, IEnumerable<string> cookies)
    {
        var sessionToken = Guid.NewGuid().ToString("N");
        var session = new WikiTreeSession(userId, userName, cookies.ToList(), DateTime.UtcNow);
        _sessions[sessionToken] = session;
        return sessionToken;
    }

    public WikiTreeSession? GetSession(string? sessionToken)
    {
        if (string.IsNullOrEmpty(sessionToken))
            return null;

        if (_sessions.TryGetValue(sessionToken, out var session))
        {
            // Expire after 24 hours
            if (DateTime.UtcNow - session.CreatedAt > TimeSpan.FromHours(24))
            {
                _sessions.TryRemove(sessionToken, out _);
                return null;
            }
            return session;
        }
        return null;
    }

    public bool RemoveSession(string? sessionToken)
    {
        if (string.IsNullOrEmpty(sessionToken))
            return false;
        return _sessions.TryRemove(sessionToken, out _);
    }
}

public record WikiTreeSession(
    string UserId,
    string UserName,
    List<string> Cookies,
    DateTime CreatedAt);
