using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nezabuti.Api.DTOs;
using Nezabuti.Api.Services;

namespace Nezabuti.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/admin/customers")]
public class AdminCustomersController : ControllerBase
{
    private readonly ICustomerService _customers;

    public AdminCustomersController(ICustomerService customers)
    {
        _customers = customers;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<CustomerDto>>> List(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        return Ok(await _customers.ListAsync(search, page, pageSize, ct));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CustomerDto>> Get(string id, CancellationToken ct)
    {
        var customer = await _customers.GetAsync(id, ct);
        return customer is null ? NotFound() : Ok(customer);
    }

    [HttpPost]
    public async Task<ActionResult<CustomerDto>> Create([FromBody] CreateCustomerRequest request, CancellationToken ct)
    {
        try
        {
            var created = await _customers.CreateAsync(request, ct);
            return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<CustomerDto>> Update(
        string id,
        [FromBody] UpdateCustomerRequest request,
        CancellationToken ct)
    {
        try
        {
            var updated = await _customers.UpdateAsync(id, request, ct);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var ok = await _customers.DeleteAsync(id, ct);
        return ok ? NoContent() : NotFound();
    }
}
