using Microsoft.AspNetCore.Mvc;
using SmartReconcile.API.Authentication;
using SmartReconcile.Application.Abstractions;
using SmartReconcile.Application.DTOs;

namespace SmartReconcile.API.Controllers;

[ApiController]
[Route("api/v1/integrations")]
public sealed class IntegrationsController(
    IIntegrationService service,
    ITenantRepository tenants) : ControllerBase
{
    [HttpPost("tenants")]
    public async Task<ActionResult<TenantDto>> CreateTenant(CreateTenantRequest request, CancellationToken ct)
    {
        try
        {
            return Ok(await service.CreateTenantAsync(request, ct));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPost("connections")]
    public async Task<ActionResult<IntegrationConnectionDto>> CreateConnection(
        CreateIntegrationConnectionRequest request,
        CancellationToken ct)
    {
        try
        {
            return Ok(await service.CreateConnectionAsync(request, ct));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("connections")]
    public async Task<ActionResult<IReadOnlyList<IntegrationConnectionDto>>> GetCurrentTenantConnections(
        CancellationToken ct)
    {
        var tenant = await ResolveCurrentTenantAsync(ct);

        if (tenant is null || !tenant.IsActive)
            return Unauthorized();

        return Ok(await service.GetConnectionsAsync(tenant.Id, ct));
    }

    [HttpGet("tenants/{tenantId:guid}/connections")]
    public async Task<ActionResult<IReadOnlyList<IntegrationConnectionDto>>> GetConnections(
        Guid tenantId,
        CancellationToken ct)
    {
        var tenant = await ResolveCurrentTenantAsync(ct);

        if (tenant is null || tenant.Id != tenantId || !tenant.IsActive)
            return Unauthorized();

        return Ok(await service.GetConnectionsAsync(tenant.Id, ct));
    }

    private async Task<SmartReconcile.Domain.Entities.Tenant?> ResolveCurrentTenantAsync(CancellationToken ct)
    {
        var tenant = ShopifyAuthenticationContext.GetTenant(HttpContext);

        if (tenant is null && ShopifyAuthenticationContext.IsDemoMode(HttpContext))
        {
            tenant = await tenants.GetBySlugAsync("smartreconcile-demo", ct);
        }

        return tenant;
    }
}
