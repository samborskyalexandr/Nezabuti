using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nezabuti.Api.DTOs;
using Nezabuti.Api.Services;

namespace Nezabuti.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/admin/plans")]
public class AdminPlansController : ControllerBase
{
    private readonly IPlanService _plans;

    public AdminPlansController(IPlanService plans)
    {
        _plans = plans;
    }

    [HttpGet]
    public async Task<ActionResult<List<PlanDto>>> List(CancellationToken ct)
    {
        return Ok(await _plans.ListAdminAsync(ct));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<PlanDto>> Update(string id, [FromBody] UpdatePlanRequest request, CancellationToken ct)
    {
        var updated = await _plans.UpdateAsync(id, request, ct);
        return updated is null ? NotFound() : Ok(updated);
    }
}
