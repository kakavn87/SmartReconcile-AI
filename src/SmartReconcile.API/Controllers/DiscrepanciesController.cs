using Microsoft.AspNetCore.Mvc;
using SmartReconcile.API.Authentication;
using SmartReconcile.Application.Abstractions;
using SmartReconcile.Application.DTOs;

namespace SmartReconcile.API.Controllers;

[ApiController]
[Route("api/discrepancies")]
public sealed class DiscrepanciesController(IDiscrepancyService service) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<IReadOnlyList<DiscrepancyDto>>> Get(CancellationToken ct)
    {
        var tenant = ShopifyAuthenticationContext.GetTenant(HttpContext);
        return tenant is null ? Unauthorized() : Ok(await service.GetAllAsync(tenant.Id, ct));
    }

    [HttpGet("{id:guid}")] public async Task<ActionResult<DiscrepancyDto>> GetById(Guid id, CancellationToken ct)
    {
        var tenant = ShopifyAuthenticationContext.GetTenant(HttpContext);
        if (tenant is null) return Unauthorized();
        return await service.GetByIdAsync(tenant.Id, id, ct) is { } x ? Ok(x) : NotFound();
    }

    [HttpPatch("{id:guid}/status")] public async Task<ActionResult<DiscrepancyDto>> UpdateStatus(Guid id, UpdateDiscrepancyStatusRequest request, CancellationToken ct)
    {
        var tenant = ShopifyAuthenticationContext.GetTenant(HttpContext);
        if (tenant is null) return Unauthorized();
        return await service.UpdateStatusAsync(tenant.Id, id, request, ct) is { } x ? Ok(x) : NotFound();
    }
}
