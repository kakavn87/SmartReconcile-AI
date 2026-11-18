using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SmartReconcile.API.Authentication;
using SmartReconcile.Application.Abstractions;
using SmartReconcile.Application.DTOs;

namespace SmartReconcile.API.Controllers;

[ApiController]
[Route("api/orders")]
public sealed class OrdersController(
    IOrderService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OrderDto>>> Get(CancellationToken ct)
    {
        var tenant = ShopifyAuthenticationContext.GetTenant(HttpContext);
        return tenant is null
            ? Unauthorized()
            : Ok(await service.GetAllAsync(tenant.Id, ct));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderDto>> GetById(Guid id, CancellationToken ct)
    {
        var tenant = ShopifyAuthenticationContext.GetTenant(HttpContext);
        if (tenant is null)
            return Unauthorized();

        var result = await service.GetByIdAsync(id, tenant.Id, ct);
        return result is { } x ? Ok(x) : NotFound();
    }

    [HttpPost]
    public async Task<ActionResult<OrderDto>> Create(CreateOrderRequest request, CancellationToken ct)
    {
        var tenant = ShopifyAuthenticationContext.GetTenant(HttpContext);
        if (tenant is null)
            return Unauthorized();

        var result = await service.CreateAsync(request, tenant.Id, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }
}
