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
        return Ok(await _settings.UpdateAsync(request ?? new UpdateSiteSettingsRequest(), ct));
    }
}
