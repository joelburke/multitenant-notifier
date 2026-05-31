using Microsoft.AspNetCore.Mvc;
using NotificationPlatform.Application.DTOs;
using NotificationPlatform.Application.Services;

namespace NotificationPlatform.Api.Controllers;

[ApiController]
[Route("api/tenants/{tenantId:guid}/logs")]
public class NotificationLogsController(EventIngestionService ingestionService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<NotificationLogResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLogs(
        Guid tenantId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default) =>
        Ok(await ingestionService.GetLogsAsync(tenantId, page, Math.Clamp(pageSize, 1, 200), ct));
}
