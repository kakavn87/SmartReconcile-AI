using Microsoft.AspNetCore.Mvc;
using SmartReconcile.Application.Abstractions;
using SmartReconcile.Application.DTOs;

namespace SmartReconcile.API.Controllers;

[ApiController]
[Route("api/v1/integrations")]
public sealed class IntegrationsController(IIntegrationService service) : ControllerBase
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

    [HttpGet("tenants/{tenantId:guid}/connections")]
    public async Task<ActionResult<IReadOnlyList<IntegrationConnectionDto>>> GetConnections(
        Guid tenantId,
        CancellationToken ct)
        => Ok(await service.GetConnectionsAsync(tenantId, ct));
}
