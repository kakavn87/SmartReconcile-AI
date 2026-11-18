using Microsoft.AspNetCore.Mvc;
using SmartReconcile.API.Authentication;

namespace SmartReconcile.API.Controllers;

[ApiController]
[Route("api/auth/shopify")]
public sealed class ShopifyAuthenticationController(
    IShopifyAuthenticationService authentication) : ControllerBase
{
    [HttpGet("session")]
    public IActionResult Session()
    {
        var session = ShopifyAuthenticationContext.GetSession(HttpContext);
        if (session is null)
        {
            if (ShopifyAuthenticationContext.IsDemoMode(HttpContext))
            {
                return Ok(new
                {
                    mode = "demo",
                    authenticated = false
                });
            }

            return Unauthorized();
        }

        return Ok(new
        {
            mode = "shopify",
            authenticated = true,
            shopDomain = session.ShopDomain,
            userId = session.UserId,
            sessionId = session.SessionId,
            expiresAtUtc = session.ExpiresAtUtc
        });
    }
}
