using Microsoft.AspNetCore.Mvc;
using FamilyTreeApi.Services;

namespace FamilyTreeApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GedcomController : ControllerBase
{
    private readonly IGedcomParserService _parserService;

    public GedcomController(IGedcomParserService parserService)
    {
        _parserService = parserService;
    }

    [HttpPost("upload")]
    [RequestSizeLimit(50 * 1024 * 1024)]
    public IActionResult Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "No file provided." });

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension != ".ged")
            return BadRequest(new { error = "Invalid file type. Please upload a .ged file." });

        try
        {
            using var stream = file.OpenReadStream();
            var result = _parserService.Parse(stream);

            if (result.Individuals.Count == 0)
                return BadRequest(new { error = "No individuals found in the GEDCOM file." });

            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = $"Failed to parse GEDCOM file: {ex.Message}" });
        }
    }
}
