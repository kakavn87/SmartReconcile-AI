using Microsoft.AspNetCore.Mvc;
using SmartReconcile.Application.Abstractions;

namespace SmartReconcile.API.Controllers;

[ApiController]
[Route("api/integrations/shopify")]
public sealed class ShopifySyncController(IShopifySyncService syncService) : ControllerBase
{
    [HttpPost("{connectionId:guid}/sync")]
    public async Task<IActionResult> Sync(Guid connectionId, CancellationToken cancellationToken)
    {
        try
        {
            var result = await syncService.SyncAsync(connectionId, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Shopify-Verbindung wurde nicht gefunden." });
        }
    }
}
