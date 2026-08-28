using Microsoft.AspNetCore.Mvc;
using Nezabuti.Api.DTOs;
using Nezabuti.Api.Services;

namespace Nezabuti.Api.Controllers;

[ApiController]
[Route("api/public/memorials")]
public class PublicMemorialsController : ControllerBase
{
    private readonly IMemorialService _memorials;
    private readonly IStatisticsService _stats;

    public PublicMemorialsController(IMemorialService memorials, IStatisticsService stats)
    {
        _memorials = memorials;
        _stats = stats;
    }

    [HttpGet("{publicId}")]
    public async Task<ActionResult<PublicMemorialDto>> Get(string publicId, CancellationToken ct)
    {
        var memorial = await _memorials.GetPublicAsync(publicId, ct);
        if (memorial is null)
        {
            return NotFound();
        }

        return Ok(memorial);
    }

    [HttpPost("{publicId}/views")]
    public async Task<IActionResult> RecordView(string publicId, [FromBody] RecordViewRequest? request, CancellationToken ct)
    {
        var memorial = await _memorials.GetPublicAsync(publicId, ct);
        if (memorial is null)
        {
            return NotFound();
        }

        await _stats.RecordPublicViewAsync(publicId, request?.IsAdminPreview ?? false, ct);
        return NoContent();
    }
}
