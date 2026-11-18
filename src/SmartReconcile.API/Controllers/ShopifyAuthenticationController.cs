using Microsoft.AspNetCore.Mvc;
using SmartReconcile.API.Authentication;

namespace SmartReconcile.API.Controllers;

[ApiController]
[Route("api/auth/shopify")]
public sealed class ShopifyAuthenticationController(IConfiguration configuration) : ControllerBase
{
    [HttpGet("config")]
    public IActionResult Config()
    {
        return Ok(new
        {
            clientId = configuration["Shopify:ClientId"] ?? string.Empty
        });
    }

    [HttpGet("session")]
    public IActionResult Session()
    {
        var session = ShopifyAuthenticationContext.GetSession(HttpContext);
        if (session is null)
            return Unauthorized(new { message = "Shopify authentication is required." });

        return Ok(new
        {
            mode = "shopify",
            authenticated = true,
            shopDomain = session.ShopDomain,
            userId = session.UserId,
            sessionId = session.SessionId,
            expiresAtUtc = session.ExpiresAtUtc,
            tenantId = ShopifyAuthenticationContext.GetTenant(HttpContext)?.Id,
            connectionId = ShopifyAuthenticationContext.GetConnection(HttpContext)?.Id
        });
    }
}
