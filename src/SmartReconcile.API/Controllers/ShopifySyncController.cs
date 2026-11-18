using Microsoft.AspNetCore.Mvc;
using SmartReconcile.API.Authentication;
using SmartReconcile.Application.Abstractions;

namespace SmartReconcile.API.Controllers;

[ApiController]
[Route("api/integrations/shopify")]
public sealed class ShopifySyncController(
    IShopifySyncService syncService,
    IShopifyOAuthService oauth,
    ISecretProtector secretProtector) : ControllerBase
{
    [HttpPost("{connectionId:guid}/sync")]
    public async Task<IActionResult> Sync(Guid connectionId, CancellationToken cancellationToken)
    {
        var currentConnection = ShopifyAuthenticationContext.GetConnection(HttpContext);

        if (currentConnection is null || currentConnection.Id != connectionId)
            return Unauthorized(new { message = "Die Shopify-Verbindung gehört nicht zum aktuell authentifizierten Shop." });

        try
        {
            if (!string.IsNullOrWhiteSpace(currentConnection.ShopDomain) &&
                !string.IsNullOrWhiteSpace(currentConnection.EncryptedAccessToken))
            {
                var accessToken = secretProtector.Unprotect(currentConnection.EncryptedAccessToken);
                await oauth.RegisterWebhooksAsync(
                    currentConnection.ShopDomain,
                    accessToken,
                    cancellationToken);
            }

            var result = await syncService.SyncAsync(connectionId, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Shopify-Verbindung wurde nicht gefunden." });
        }
    }
}
