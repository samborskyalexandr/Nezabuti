using Microsoft.AspNetCore.Mvc;
using Nezabuti.Api.DTOs;
using Nezabuti.Api.Services;

namespace Nezabuti.Api.Controllers;

[ApiController]
[Route("api/public/plans")]
public class PublicPlansController : ControllerBase
{
    private readonly IPlanService _plans;

    public PublicPlansController(IPlanService plans)
    {
        _plans = plans;
    }

    [HttpGet]
    public async Task<ActionResult<List<PublicPlanDto>>> List(CancellationToken ct)
    {
        return Ok(await _plans.ListPublicAsync(ct));
    }
}
