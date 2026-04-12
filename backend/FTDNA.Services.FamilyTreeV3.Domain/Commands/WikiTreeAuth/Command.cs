namespace FTDNA.Services.FamilyTreeV3.Domain.Commands.WikiTreeAuth;

public record ConfirmWikiTreeAuthCommand(string AuthCode);

public record WikiTreeAuthResult(string SessionToken, string UserId, string UserName);

public record WikiTreeLogoutCommand(string? SessionToken);

public record GetWikiTreeAuthStatusQuery(string? SessionToken);

public record WikiTreeAuthStatusResponse(bool Authenticated, string? UserId, string? UserName);
