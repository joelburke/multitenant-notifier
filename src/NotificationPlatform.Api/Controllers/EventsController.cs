using Microsoft.AspNetCore.Mvc;
using NotificationPlatform.Application.DTOs;
using NotificationPlatform.Application.Services;

namespace NotificationPlatform.Api.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController(EventIngestionService ingestionService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(IngestEventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Ingest([FromBody] IngestEventRequest request, CancellationToken ct) =>
        Ok(await ingestionService.IngestAsync(request, ct));
}
