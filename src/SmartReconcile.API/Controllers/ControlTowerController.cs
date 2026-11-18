using Microsoft.AspNetCore.Mvc;
using SmartReconcile.API.Authentication;
using SmartReconcile.Application.Abstractions;
using SmartReconcile.Application.DTOs;
using SmartReconcile.Domain.Enums;

namespace SmartReconcile.API.Controllers;

[ApiController]
[Route("api/control-tower")]
public sealed class ControlTowerController(IControlTowerService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ControlTowerDto>> Get(CancellationToken ct)
    {
        var tenant = ShopifyAuthenticationContext.GetTenant(HttpContext);
        return tenant is null ? Unauthorized() : Ok(await service.GetOverviewAsync(tenant.Id, ct));
    }

    [HttpGet("cases/{id:guid}")]
    public async Task<ActionResult<FinancialCaseDetailDto>> GetCase(Guid id, CancellationToken ct)
    {
        var tenant = ShopifyAuthenticationContext.GetTenant(HttpContext);
        if (tenant is null) return Unauthorized();
        var result = await service.GetCaseDetailAsync(tenant.Id, id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("cases/{id:guid}/investigate")]
    public async Task<ActionResult<FinancialCaseDto>> Investigate(Guid id, CancellationToken ct)
    {
        var tenant = ShopifyAuthenticationContext.GetTenant(HttpContext);
        if (tenant is null) return Unauthorized();
        var result = await service.StartInvestigationAsync(tenant.Id, id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("cases/{id:guid}/auto-fix")]
    public async Task<ActionResult<FinancialCaseDto>> AutoFix(Guid id, CancellationToken ct)
    {
        var tenant = ShopifyAuthenticationContext.GetTenant(HttpContext);
        if (tenant is null) return Unauthorized();
        var result = await service.AutoFixAsync(tenant.Id, id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("cases/{id:guid}/resolve")]
    public async Task<ActionResult<FinancialCaseDto>> Resolve(Guid id, [FromBody] ResolveFinancialCaseRequest request, CancellationToken ct)
    {
        var tenant = ShopifyAuthenticationContext.GetTenant(HttpContext);
        if (tenant is null) return Unauthorized();
        var result = await service.ResolveAsync(tenant.Id, id, request.RootCause, request.Note, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("cases/{id:guid}/ignore")]
    public async Task<ActionResult<FinancialCaseDto>> Ignore(Guid id, CancellationToken ct)
    {
        var tenant = ShopifyAuthenticationContext.GetTenant(HttpContext);
        if (tenant is null) return Unauthorized();
        var result = await service.IgnoreAsync(tenant.Id, id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("graph/{nodeType}/{entityId:guid}")]
    public async Task<ActionResult<List<FinancialGraphRelationDto>>> Graph(FinancialNodeType nodeType, Guid entityId, CancellationToken ct)
    {
        var tenant = ShopifyAuthenticationContext.GetTenant(HttpContext);
        if (tenant is null) return Unauthorized();
        return Ok(await service.GetGraphAsync(tenant.Id, nodeType, entityId, ct));
    }
}

public sealed record ResolveFinancialCaseRequest(string? RootCause, string? Note);
