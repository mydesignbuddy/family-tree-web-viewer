using Microsoft.AspNetCore.Mvc;
using Wolverine;
using FTDNA.Services.FamilyTreeV3.Common.Models;
using FTDNA.Services.FamilyTreeV3.Domain.Commands.ClearFamilyTree;
using FTDNA.Services.FamilyTreeV3.Domain.Commands.ActivateTree;
using FTDNA.Services.FamilyTreeV3.Domain.Commands.DeleteTree;
using FTDNA.Services.FamilyTreeV3.Domain.Queries.GetCurrentTree;
using FTDNA.Services.FamilyTreeV3.Domain.Queries.GetTreeSubset;
using FTDNA.Services.FamilyTreeV3.Domain.Queries.ListTrees;

namespace FTDNA.Services.FamilyTreeV3.Api.Controllers;

/// <summary>
/// Shared tree operations that apply regardless of data source (GEDCOM, WikiTree, etc.).
/// </summary>
public abstract class FamilyTreeControllerBase : ControllerBase
{
    protected const int DefaultAncestorDepth = 5;
    protected const int DefaultDescendantDepth = 2;

    protected readonly IMessageBus Bus;

    protected FamilyTreeControllerBase(IMessageBus bus)
    {
        Bus = bus;
    }

    [HttpGet("current")]
    public async Task<IActionResult> GetCurrent()
    {
        var result = await Bus.InvokeAsync<FluentResults.Result<FamilyTreeModel>>(
            new GetCurrentTreeQuery(DefaultAncestorDepth, DefaultDescendantDepth));

        if (result.IsFailed)
            return NotFound(new { error = result.Errors[0].Message });

        return Ok(result.Value);
    }

    [HttpGet("trees")]
    public async Task<IActionResult> ListTrees()
    {
        var result = await Bus.InvokeAsync<TreeListResponse>(new ListTreesQuery());
        return Ok(result);
    }

    [HttpPost("trees/{treeId}/activate")]
    public async Task<IActionResult> ActivateTree(string treeId)
    {
        var result = await Bus.InvokeAsync<FluentResults.Result<FamilyTreeModel>>(
            new ActivateTreeCommand(treeId, DefaultAncestorDepth, DefaultDescendantDepth));

        if (result.IsFailed)
            return NotFound(new { error = result.Errors[0].Message });

        return Ok(result.Value);
    }

    [HttpDelete("trees/{treeId}")]
    public async Task<IActionResult> DeleteTree(string treeId)
    {
        var result = await Bus.InvokeAsync<FluentResults.Result<TreeListResponse>>(
            new DeleteTreeCommand(treeId));

        if (result.IsFailed)
            return NotFound(new { error = result.Errors[0].Message });

        return Ok(result.Value);
    }

    [HttpDelete("current")]
    public async Task<IActionResult> ClearCurrent()
    {
        await Bus.InvokeAsync<FluentResults.Result>(new ClearFamilyTreeCommand());
        return NoContent();
    }

    protected async Task<IActionResult> ExpandTree(string personId, int? ancestorDepth, int? descendantDepth)
    {
        if (string.IsNullOrWhiteSpace(personId))
            return BadRequest(new { error = "Person ID is required." });

        var depth = Math.Clamp(ancestorDepth ?? DefaultAncestorDepth, 1, 10);
        var descDepth = Math.Clamp(descendantDepth ?? DefaultDescendantDepth, 0, 10);

        var result = await Bus.InvokeAsync<FluentResults.Result<FamilyTreeModel>>(
            new GetTreeSubsetQuery(personId, depth, descDepth));

        if (result.IsFailed)
            return NotFound(new { error = result.Errors[0].Message });

        return Ok(result.Value);
    }
}
