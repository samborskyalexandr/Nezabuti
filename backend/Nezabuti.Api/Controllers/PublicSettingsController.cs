using Microsoft.AspNetCore.Mvc;
using Nezabuti.Api.DTOs;
using Nezabuti.Api.Services;

namespace Nezabuti.Api.Controllers;

[ApiController]
[Route("api/public/settings")]
public class PublicSettingsController : ControllerBase
{
    private readonly ISiteSettingsService _settings;

    public PublicSettingsController(ISiteSettingsService settings)
    {
        _settings = settings;
    }

    [HttpGet]
    public async Task<ActionResult<PublicSiteSettingsDto>> Get(CancellationToken ct)
    {
        return Ok(await _settings.GetPublicAsync(ct));
    }
}
