using Microsoft.AspNetCore.Mvc;
using Wolverine;
using FTDNA.Services.FamilyTreeV3.Common.Models;
using FTDNA.Services.FamilyTreeV3.Domain.Commands.ImportWikiTree;
using FTDNA.Services.FamilyTreeV3.Domain.Commands.ExpandWikiTree;
using FTDNA.Services.FamilyTreeV3.Domain.Commands.WikiTreeAuth;

namespace FTDNA.Services.FamilyTreeV3.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WikiTreeController : FamilyTreeControllerBase
{
    private const string SessionCookieName = "wikitree_session";

    public WikiTreeController(IMessageBus bus) : base(bus) { }

    [HttpPost("import")]
    public async Task<IActionResult> Import([FromBody] WikiTreeImportRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.WikiTreeId))
            return BadRequest(new { error = "WikiTree ID is required." });

        var sessionToken = Request.Cookies[SessionCookieName];

        var result = await Bus.InvokeAsync<FluentResults.Result<FamilyTreeModel>>(
            new ImportWikiTreeCommand(
                request.WikiTreeId,
                request.AncestorDepth ?? DefaultAncestorDepth,
                request.DescendantDepth ?? DefaultDescendantDepth,
                sessionToken));

        if (result.IsFailed)
            return BadRequest(new { error = result.Errors[0].Message });

        return Ok(result.Value);
    }

    [HttpPost("expand")]
    public async Task<IActionResult> Expand([FromBody] WikiTreeExpandRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.IndividualId))
            return BadRequest(new { error = "Individual ID is required." });

        var sessionToken = Request.Cookies[SessionCookieName];
        var depth = Math.Clamp(request.Depth ?? DefaultAncestorDepth, 1, 10);

        var result = await Bus.InvokeAsync<FluentResults.Result<FamilyTreeModel>>(
            new ExpandWikiTreeCommand(request.IndividualId, depth, sessionToken));

        if (result.IsFailed)
            return BadRequest(new { error = result.Errors[0].Message });

        return Ok(result.Value);
    }

    [HttpGet("auth/login")]
    public IActionResult GetLoginUrl([FromQuery] string returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
            return BadRequest(new { error = "returnUrl is required." });

        var loginUrl = $"https://api.wikitree.com/api.php?action=clientLogin&returnURL={Uri.EscapeDataString(returnUrl)}&appId=FamilyTreeWebViewer";
        return Ok(new { loginUrl });
    }

    [HttpPost("auth/callback")]
    public async Task<IActionResult> AuthCallback([FromBody] WikiTreeAuthCallbackRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.AuthCode))
            return BadRequest(new { error = "authcode is required." });

        var result = await Bus.InvokeAsync<FluentResults.Result<WikiTreeAuthResult>>(
            new ConfirmWikiTreeAuthCommand(request.AuthCode));

        if (result.IsFailed)
            return BadRequest(new { error = result.Errors[0].Message });

        Response.Cookies.Append(SessionCookieName, result.Value.SessionToken, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Secure = false,
            MaxAge = TimeSpan.FromHours(24),
            Path = "/api/wikitree"
        });

        return Ok(new
        {
            userId = result.Value.UserId,
            userName = result.Value.UserName
        });
    }

    [HttpGet("auth/status")]
    public async Task<IActionResult> AuthStatus()
    {
        var sessionToken = Request.Cookies[SessionCookieName];
        var status = await Bus.InvokeAsync<WikiTreeAuthStatusResponse>(
            new GetWikiTreeAuthStatusQuery(sessionToken));

        if (!status.Authenticated)
            return Ok(new { authenticated = false });

        return Ok(new
        {
            authenticated = true,
            userId = status.UserId,
            userName = status.UserName
        });
    }

    [HttpPost("auth/logout")]
    public async Task<IActionResult> Logout()
    {
        var sessionToken = Request.Cookies[SessionCookieName];
        await Bus.InvokeAsync(new WikiTreeLogoutCommand(sessionToken));

        Response.Cookies.Delete(SessionCookieName, new CookieOptions
        {
            Path = "/api/wikitree"
        });

        return Ok(new { success = true });
    }
}

public record WikiTreeImportRequest(string WikiTreeId, int? AncestorDepth, int? DescendantDepth);
public record WikiTreeAuthCallbackRequest(string AuthCode);
public record WikiTreeExpandRequest(string IndividualId, int? Depth);
