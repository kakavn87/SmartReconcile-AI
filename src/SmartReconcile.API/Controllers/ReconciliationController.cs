using Microsoft.AspNetCore.Mvc;
using SmartReconcile.API.Authentication;
using SmartReconcile.Application.Abstractions;
using SmartReconcile.Application.DTOs;

namespace SmartReconcile.API.Controllers;

[ApiController]
[Route("api/reconciliation")]
public sealed class ReconciliationController(IReconciliationService service) : ControllerBase
{
    [HttpPost("run")]
    public async Task<ActionResult<ReconciliationResult>> Run(CancellationToken ct)
    {
        var tenant = ShopifyAuthenticationContext.GetTenant(HttpContext);
        return tenant is null ? Unauthorized() : Ok(await service.ReconcileAsync(tenant.Id, ct));
    }
}
