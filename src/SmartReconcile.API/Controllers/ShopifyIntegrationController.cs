using Microsoft.AspNetCore.Mvc;
using SmartReconcile.API.Integrations;
using SmartReconcile.Application.Abstractions;
using SmartReconcile.Domain.Entities;
using SmartReconcile.Domain.Enums;

namespace SmartReconcile.API.Controllers;

[ApiController]
[Route("api/integrations/shopify")]
public sealed class ShopifyIntegrationController(
    IShopifyOAuthService oauth,
    ShopifyOAuthStateStore stateStore,
    ITenantRepository tenants,
    IIntegrationConnectionRepository connections,
    ISecretProtector secretProtector) : ControllerBase
{
    [HttpGet("install")]
    public IActionResult Install([FromQuery] string shop)
    {
        if (!oauth.IsValidShopDomain(shop))
            return BadRequest(new { message = "Ungültige Shopify-Shop-Domain." });

        var state = stateStore.Create();
        Response.Cookies.Append("sr_shopify_oauth_state", state, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            MaxAge = TimeSpan.FromMinutes(10),
            IsEssential = true
        });
        return Redirect(oauth.BuildInstallUrl(shop.Trim().ToLowerInvariant(), state));
    }

    [HttpGet("callback")]
    public async Task<IActionResult> Callback(
        [FromQuery] string shop,
        [FromQuery] string code,
        [FromQuery] string state,
        CancellationToken cancellationToken)
    {
        var query = Request.Query.ToDictionary(
            x => x.Key,
            x => x.Value.Count > 0 ? x.Value[0] : null,
            StringComparer.Ordinal);

        if (!oauth.IsValidShopDomain(shop))
            return BadRequest(new { message = "Ungültige Shopify-Shop-Domain." });

        var stateCookie = Request.Cookies["sr_shopify_oauth_state"];
        Response.Cookies.Delete("sr_shopify_oauth_state");
        if (!string.Equals(stateCookie, state, StringComparison.Ordinal) || !stateStore.Consume(state))
            return Unauthorized(new { message = "Ungültiger oder abgelaufener OAuth-State." });

        if (!oauth.VerifyCallbackHmac(query))
            return Unauthorized(new { message = "Shopify-HMAC konnte nicht verifiziert werden." });

        var tenant = await tenants.GetByIdAsync(DefaultTenantId, cancellationToken);
        if (tenant is null)
            return Problem("Demo-Mandant wurde nicht gefunden.");

        var normalizedShop = shop.Trim().ToLowerInvariant();
        var token = await oauth.ExchangeCodeAsync(normalizedShop, code, cancellationToken);
        await oauth.RegisterWebhooksAsync(normalizedShop, token.AccessToken, cancellationToken);
        var existing = await connections.GetForTenantAsync(tenant.Id, cancellationToken);
        var connection = existing.FirstOrDefault(x =>
            x.Provider == IntegrationProvider.Shopify &&
            string.Equals(x.ShopDomain, shop.Trim(), StringComparison.OrdinalIgnoreCase));

        if (connection is null)
        {
            connection = IntegrationConnection.CreateShopify(
                tenant.Id,
                shop,
                shop,
                secretProtector.Protect(token.AccessToken),
                token.Scope,
                string.IsNullOrWhiteSpace(token.RefreshToken) ? null : string.IsNullOrWhiteSpace(token.RefreshToken) ? null : secretProtector.Protect(token.RefreshToken),
                DateTime.UtcNow.AddSeconds(token.ExpiresInSeconds),
                DateTime.UtcNow.AddSeconds(token.RefreshTokenExpiresInSeconds));
            await connections.AddAsync(connection, cancellationToken);
        }
        else
        {
            connection.UpdateShopifyCredentials(
                secretProtector.Protect(token.AccessToken),
                token.Scope,
                secretProtector.Protect(token.RefreshToken),
                DateTime.UtcNow.AddSeconds(token.ExpiresInSeconds),
                DateTime.UtcNow.AddSeconds(token.RefreshTokenExpiresInSeconds));
        }

        return Redirect($"{frontendUrl}/integrationen");
    }

    private string frontendUrl => "https://smartreconcile-ai-frontend.onrender.com";

    private static readonly Guid DefaultTenantId =
        Guid.Parse("00000000-0000-0000-0000-000000000001");
}
