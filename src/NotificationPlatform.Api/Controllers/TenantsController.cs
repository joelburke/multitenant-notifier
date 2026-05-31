using Microsoft.AspNetCore.Mvc;
using NotificationPlatform.Application.DTOs;
using NotificationPlatform.Application.Services;

namespace NotificationPlatform.Api.Controllers;

[ApiController]
[Route("api/tenants")]
public class TenantsController(TenantService tenantService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TenantResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(await tenantService.GetAllAsync(ct));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TenantResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct) =>
        Ok(await tenantService.GetByIdAsync(id, ct));

    [HttpPost]
    [ProducesResponseType(typeof(TenantResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateTenantRequestDto request, CancellationToken ct)
    {
        var tenant = await tenantService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = tenant.Id }, tenant);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(TenantResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTenantRequestDto request, CancellationToken ct) =>
        Ok(await tenantService.UpdateAsync(id, request, ct));

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await tenantService.DeleteAsync(id, ct);
        return NoContent();
    }
}
