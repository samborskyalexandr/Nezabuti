using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Nezabuti.Api.Configuration;
using Nezabuti.Api.DTOs;
using Nezabuti.Api.Models;
using Nezabuti.Api.Services;

namespace Nezabuti.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/admin/memorials")]
public class AdminMemorialsController : ControllerBase
{
    private readonly IMemorialService _memorials;
    private readonly IStatisticsService _stats;
    private readonly IQrCodeService _qr;
    private readonly ImageSettings _imageSettings;

    public AdminMemorialsController(
        IMemorialService memorials,
        IStatisticsService stats,
        IQrCodeService qr,
        IOptions<ImageSettings> imageSettings)
    {
        _memorials = memorials;
        _stats = stats;
        _qr = qr;
        _imageSettings = imageSettings.Value;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<MemorialListItemDto>>> List(
        [FromQuery] string? search,
        [FromQuery] MemorialStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _memorials.ListAsync(search, status, page, pageSize, ct);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<MemorialAdminDto>> Create([FromBody] CreateMemorialRequest request, CancellationToken ct)
    {
        var created = await _memorials.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<MemorialAdminDto>> Get(string id, CancellationToken ct)
    {
        var memorial = await _memorials.GetAdminAsync(id, ct);
        return memorial is null ? NotFound() : Ok(memorial);
    }

    /// <summary>
    /// Admin-only preview of any status. Does not affect public visibility or statistics.
    /// </summary>
    [HttpGet("{id}/preview")]
    public async Task<ActionResult<PublicMemorialDto>> Preview(string id, CancellationToken ct)
    {
        var memorial = await _memorials.GetAdminPreviewAsync(id, ct);
        return memorial is null ? NotFound() : Ok(memorial);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<MemorialAdminDto>> Update(string id, [FromBody] UpdateMemorialRequest request, CancellationToken ct)
    {
        try
        {
            var updated = await _memorials.UpdateAsync(id, request, ct);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/publish")]
    public async Task<ActionResult<MemorialAdminDto>> Publish(string id, CancellationToken ct)
    {
        try
        {
            var result = await _memorials.PublishAsync(id, ct);
            return result is null ? NotFound() : Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/archive")]
    public async Task<ActionResult<MemorialAdminDto>> Archive(string id, CancellationToken ct)
    {
        var result = await _memorials.ArchiveAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{id}/restore")]
    public async Task<ActionResult<MemorialAdminDto>> Restore(string id, CancellationToken ct)
    {
        try
        {
            var result = await _memorials.RestoreAsync(id, ct);
            return result is null ? NotFound() : Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> PermanentDelete(string id, CancellationToken ct)
    {
        var (ok, error) = await _memorials.PermanentDeleteAsync(id, ct);
        if (!ok)
        {
            return error == "Not found" ? NotFound() : BadRequest(new { message = error });
        }

        return NoContent();
    }

    [HttpPut("{id}/blocks/order")]
    public async Task<ActionResult<MemorialAdminDto>> ReorderBlocks(
        string id,
        [FromBody] ReorderBlocksRequest request,
        CancellationToken ct)
    {
        try
        {
            var result = await _memorials.ReorderBlocksAsync(id, request, ct);
            return result is null ? NotFound() : Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/photos")]
    [RequestSizeLimit(30L * 1024 * 1024)]
    public async Task<ActionResult<PhotoRefDto>> UploadPhoto(
        string id,
        IFormFile file,
        [FromQuery] bool asMainPhoto = false,
        CancellationToken ct = default)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = "Потрібен файл зображення" });
        }

        if (file.Length > _imageSettings.MaxUploadBytes)
        {
            var maxMb = Math.Max(1, _imageSettings.MaxUploadBytes / (1024 * 1024));
            return BadRequest(new { message = $"Максимальний розмір фото — {maxMb} МБ." });
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var photo = await _memorials.UploadPhotoAsync(id, stream, file.ContentType, asMainPhoto, ct);
            return photo is null ? NotFound() : Ok(photo);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}/photos/{photoId}")]
    public async Task<IActionResult> DeletePhoto(string id, string photoId, CancellationToken ct)
    {
        var (ok, error) = await _memorials.DeletePhotoAsync(id, photoId, ct);
        if (!ok)
        {
            return error == "Not found" ? NotFound() : BadRequest(new { message = error });
        }

        return NoContent();
    }

    [HttpGet("{id}/qr.png")]
    public async Task<IActionResult> QrPng(string id, CancellationToken ct)
    {
        var memorial = await _memorials.GetAdminAsync(id, ct);
        if (memorial is null)
        {
            return NotFound();
        }

        var bytes = _qr.GeneratePng(memorial.PublicId);
        return File(bytes, "image/png", $"{memorial.PublicId}.png");
    }

    [HttpGet("{id}/qr.svg")]
    public async Task<IActionResult> QrSvg(string id, CancellationToken ct)
    {
        var memorial = await _memorials.GetAdminAsync(id, ct);
        if (memorial is null)
        {
            return NotFound();
        }

        var svg = _qr.GenerateSvg(memorial.PublicId);
        return Content(svg, "image/svg+xml");
    }

    [HttpGet("{id}/statistics")]
    public async Task<ActionResult<MemorialStatisticsDto>> Statistics(string id, CancellationToken ct)
    {
        var memorial = await _memorials.GetAdminAsync(id, ct);
        if (memorial is null)
        {
            return NotFound();
        }

        var stats = await _stats.GetAsync(memorial.PublicId, ct);
        return stats is null ? NotFound() : Ok(stats);
    }
}
