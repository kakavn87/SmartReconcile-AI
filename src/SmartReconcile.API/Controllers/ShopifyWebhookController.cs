using Microsoft.AspNetCore.Mvc;
using SmartReconcile.Application.Abstractions;

namespace SmartReconcile.API.Controllers;

[ApiController]
[Route("api/webhooks/shopify")]
public sealed class ShopifyWebhookController(
    IShopifyWebhookService webhookService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Receive(CancellationToken cancellationToken)
    {
        Request.EnableBuffering();

        using var reader = new StreamReader(Request.Body);
        var rawBody = await reader.ReadToEndAsync(cancellationToken);
        Request.Body.Position = 0;

        var hmac = Request.Headers["X-Shopify-Hmac-SHA256"].FirstOrDefault();
        var shop = Request.Headers["X-Shopify-Shop-Domain"].FirstOrDefault();
        var topic = Request.Headers["X-Shopify-Topic"].FirstOrDefault();
        var webhookId = Request.Headers["X-Shopify-Webhook-Id"].FirstOrDefault();

        try
        {
            var result = await webhookService.EnqueueAsync(
                rawBody,
                hmac ?? string.Empty,
                shop,
                topic,
                webhookId,
                cancellationToken);

            return result.Duplicate ? Ok() : Ok(new
            {
                result.EventId,
                result.Topic,
                result.ShopDomain
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}
