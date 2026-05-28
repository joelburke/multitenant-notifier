using Microsoft.AspNetCore.Mvc;
using NotificationPlatform.Application.DTOs;
using NotificationPlatform.Application.Services;

namespace NotificationPlatform.Api.Controllers;

[ApiController]
[Route("api/tenants/{tenantId:guid}/rules")]
public class RoutingRulesController(RoutingRuleService ruleService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<RoutingRuleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAll(Guid tenantId, CancellationToken ct) =>
        Ok(await ruleService.GetByTenantAsync(tenantId, ct));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(RoutingRuleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid tenantId, Guid id, CancellationToken ct) =>
        Ok(await ruleService.GetByIdAsync(id, tenantId, ct));

    [HttpPost]
    [ProducesResponseType(typeof(RoutingRuleResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(Guid tenantId, [FromBody] CreateRoutingRuleRequest request, CancellationToken ct)
    {
        var rule = await ruleService.CreateAsync(tenantId, request, ct);
        return CreatedAtAction(nameof(GetById), new { tenantId, id = rule.Id }, rule);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(RoutingRuleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid tenantId, Guid id, [FromBody] UpdateRoutingRuleRequest request, CancellationToken ct) =>
        Ok(await ruleService.UpdateAsync(id, tenantId, request, ct));

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid tenantId, Guid id, CancellationToken ct)
    {
        await ruleService.DeleteAsync(id, tenantId, ct);
        return NoContent();
    }
}
