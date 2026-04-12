using FluentResults;
using FTDNA.Services.FamilyTreeV3.Data.External.Services;

namespace FTDNA.Services.FamilyTreeV3.Domain.Commands.WikiTreeAuth;

public static class ConfirmWikiTreeAuthHandler
{
    public static async Task<Result<WikiTreeAuthResult>> Handle(
        ConfirmWikiTreeAuthCommand command,
        WikiTreeApiClient wikiTree,
        WikiTreeSessionStore sessionStore,
        CancellationToken ct)
    {
        var loginResult = await wikiTree.ConfirmAuthCodeAsync(command.AuthCode, ct);

        if (!loginResult.Success)
            return Result.Fail("WikiTree authentication failed. Please try again.");

        var sessionToken = sessionStore.CreateSession(
            loginResult.UserId!,
            loginResult.UserName!,
            loginResult.Cookies);

        return Result.Ok(new WikiTreeAuthResult(sessionToken, loginResult.UserId!, loginResult.UserName!));
    }
}

public static class GetWikiTreeAuthStatusHandler
{
    public static WikiTreeAuthStatusResponse Handle(
        GetWikiTreeAuthStatusQuery query,
        WikiTreeSessionStore sessionStore)
    {
        var session = sessionStore.GetSession(query.SessionToken);
        if (session == null)
            return new WikiTreeAuthStatusResponse(false, null, null);

        return new WikiTreeAuthStatusResponse(true, session.UserId, session.UserName);
    }
}

public static class WikiTreeLogoutHandler
{
    public static void Handle(
        WikiTreeLogoutCommand command,
        WikiTreeSessionStore sessionStore)
    {
        sessionStore.RemoveSession(command.SessionToken);
    }
}
