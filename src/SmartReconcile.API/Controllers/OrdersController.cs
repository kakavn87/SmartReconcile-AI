using Microsoft.AspNetCore.Mvc;
using SmartReconcile.API.Authentication;
using SmartReconcile.Application.Abstractions;
using SmartReconcile.Application.DTOs;

namespace SmartReconcile.API.Controllers;

[ApiController]
[Route("api/orders")]
public sealed class OrdersController(
    IOrderService service,
    IShopifySyncService shopifySync) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OrderDto>>> Get(CancellationToken ct)
    {
        var connection = ShopifyAuthenticationContext.GetConnection(HttpContext);
        if (connection?.Provider == SmartReconcile.Domain.Enums.IntegrationProvider.Shopify)
        {
            try
            {
                await shopifySync.SyncAsync(connection.Id, ct);
            }
            catch
            {
                // The orders list must remain available from PostgreSQL even when
                // Shopify is temporarily unavailable.
            }
        }

        var tenant = ShopifyAuthenticationContext.GetTenant(HttpContext);
        return tenant is null
            ? Unauthorized()
            : Ok(await service.GetAllAsync(tenant.Id, ct));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderDto>> GetById(Guid id, CancellationToken ct)
    {
        var tenant = ShopifyAuthenticationContext.GetTenant(HttpContext);
        return tenant is null
            ? Unauthorized()
            : await service.GetByIdAsync(id, tenant.Id, ct) is { } x ? Ok(x) : NotFound();
    }

    [HttpPost]
    public async Task<ActionResult<OrderDto>> Create(CreateOrderRequest request, CancellationToken ct)
    {
        var result = await service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }
}
