using Microsoft.AspNetCore.Mvc;
using Prometheus;
using Wolverine;
using FTDNA.Services.FamilyTreeV3.Common.Models;
using FTDNA.Services.FamilyTreeV3.Domain.Commands.UploadGedcom;
using FTDNA.Services.FamilyTreeV3.Domain.Queries.GetAncestorChain;

namespace FTDNA.Services.FamilyTreeV3.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GedcomController : FamilyTreeControllerBase
{
    private static readonly Counter GedcomUploadsTotal = Metrics
        .CreateCounter("gedcom_uploads_total", "Total number of GEDCOM uploads",
            labelNames: ["result"]);

    private static readonly Histogram GedcomParseDuration = Metrics
        .CreateHistogram("gedcom_parse_duration_seconds", "GEDCOM parse duration in seconds",
            new HistogramConfiguration { Buckets = Histogram.ExponentialBuckets(0.1, 2, 10) });

    private static readonly Gauge GedcomIndividualsLoaded = Metrics
        .CreateGauge("gedcom_individuals_loaded", "Number of individuals in the last loaded GEDCOM");

    private readonly ILogger<GedcomController> _logger;

    public GedcomController(IMessageBus bus, ILogger<GedcomController> logger) : base(bus)
    {
        _logger = logger;
    }

    [HttpPost("upload")]
    [RequestSizeLimit(50 * 1024 * 1024)]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "No file provided." });

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension != ".ged")
            return BadRequest(new { error = "Invalid file type. Please upload a .ged file." });

        _logger.LogInformation("GEDCOM upload started for file {FileName} ({FileSize} bytes)",
            file.FileName, file.Length);

        using var timer = GedcomParseDuration.NewTimer();
        using var stream = file.OpenReadStream();

        var result = await Bus.InvokeAsync<FluentResults.Result<FamilyTreeModel>>(
            new UploadGedcomCommand(stream, file.FileName));

        if (result.IsFailed)
        {
            GedcomUploadsTotal.WithLabels("failure").Inc();
            _logger.LogWarning("GEDCOM upload failed for {FileName}: {Error}",
                file.FileName, result.Errors[0].Message);
            return BadRequest(new { error = result.Errors[0].Message });
        }

        var individualCount = result.Value.Individuals?.Count ?? 0;
        GedcomUploadsTotal.WithLabels("success").Inc();
        GedcomIndividualsLoaded.Set(individualCount);

        _logger.LogInformation("GEDCOM upload succeeded for {FileName}: {IndividualCount} individuals loaded",
            file.FileName, individualCount);

        return Ok(result.Value);
    }

    [HttpPost("expand")]
    public Task<IActionResult> Expand([FromBody] GedcomExpandRequest request)
        => ExpandTree(request.PersonId, request.AncestorDepth, request.DescendantDepth);

    [HttpGet("debug/{personId}")]
    public async Task<IActionResult> DebugAncestors(string personId)
    {
        var result = await Bus.InvokeAsync<FluentResults.Result<AncestorChainResponse>>(
            new GetAncestorChainQuery(personId));

        if (result.IsFailed)
            return NotFound(new { error = result.Errors[0].Message });

        return Ok(result.Value);
    }
}

public record GedcomExpandRequest(string PersonId, int? AncestorDepth, int? DescendantDepth);
