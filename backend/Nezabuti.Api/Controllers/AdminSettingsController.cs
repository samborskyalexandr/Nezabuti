using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nezabuti.Api.DTOs;
using Nezabuti.Api.Services;

namespace Nezabuti.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/admin/settings")]
public class AdminSettingsController : ControllerBase
{
    private readonly ISiteSettingsService _settings;

    public AdminSettingsController(ISiteSettingsService settings)
    {
        _settings = settings;
    }

    [HttpGet]
    public async Task<ActionResult<SiteSettingsDto>> Get(CancellationToken ct)
    {
        return Ok(await _settings.GetAsync(ct));
    }

    [HttpPut]
    public async Task<ActionResult<SiteSettingsDto>> Put([FromBody] UpdateSiteSettingsRequest request, CancellationToken ct)
    {
        try
        {
            return Ok(await _settings.UpdateAsync(request ?? new UpdateSiteSettingsRequest(), ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("test-telegram")]
    public async Task<ActionResult<TelegramTestResultDto>> TestTelegram(CancellationToken ct)
    {
        return Ok(await _settings.TestTelegramAsync(ct));
    }

    [HttpPost("home-images")]
    [RequestSizeLimit(30L * 1024 * 1024)]
    public async Task<ActionResult<PhotoRefDto>> UploadHomeImage(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = "Потрібен файл зображення" });
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var photo = await _settings.UploadHomeImageAsync(stream, file.ContentType, ct);
            return Ok(photo);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
