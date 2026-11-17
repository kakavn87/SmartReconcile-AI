using Microsoft.AspNetCore.Mvc;
using SmartReconcile.Application.Abstractions;
using SmartReconcile.Application.DTOs;

namespace SmartReconcile.API.Controllers;

[ApiController]
[Route("api/v1/webhooks")]
public sealed class WebhooksController(IIntegrationService service) : ControllerBase
{
    [HttpPost("{provider}")]
    public async Task<ActionResult<WebhookAcceptedDto>> Receive(
        string provider,
        [FromHeader(Name = "X-SmartReconcile-Api-Key")] string? apiKey,
        [FromHeader(Name = "X-External-Event-Id")] string? externalEventId,
        [FromHeader(Name = "X-Event-Type")] string? eventType,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            return Unauthorized(new { message = "Missing X-SmartReconcile-Api-Key." });

        if (string.IsNullOrWhiteSpace(externalEventId))
            return BadRequest(new { message = "Missing X-External-Event-Id." });

        if (string.IsNullOrWhiteSpace(eventType))
            return BadRequest(new { message = "Missing X-Event-Type." });

        Request.EnableBuffering();
        using var reader = new StreamReader(Request.Body);
        var payloadJson = await reader.ReadToEndAsync(ct);

        try
        {
            var result = await service.ReceiveWebhookAsync(
                provider,
                apiKey,
                externalEventId,
                eventType,
                payloadJson,
                ct);

            return result.Duplicate
                ? Ok(result)
                : Accepted(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }
}
