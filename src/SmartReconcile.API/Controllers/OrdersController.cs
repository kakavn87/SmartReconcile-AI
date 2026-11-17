using Microsoft.AspNetCore.Mvc;
using SmartReconcile.Application.Abstractions;
using SmartReconcile.Application.DTOs;

namespace SmartReconcile.API.Controllers;

[ApiController]
[Route("api/orders")]
public sealed class OrdersController(IOrderService service) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<IReadOnlyList<OrderDto>>> Get(CancellationToken ct) => Ok(await service.GetAllAsync(ct));
    [HttpGet("{id:guid}")] public async Task<ActionResult<OrderDto>> GetById(Guid id, CancellationToken ct) => await service.GetByIdAsync(id, ct) is { } x ? Ok(x) : NotFound();
    [HttpPost] public async Task<ActionResult<OrderDto>> Create(CreateOrderRequest request, CancellationToken ct) { var result = await service.CreateAsync(request, ct); return CreatedAtAction(nameof(GetById), new { id = result.Id }, result); }
}