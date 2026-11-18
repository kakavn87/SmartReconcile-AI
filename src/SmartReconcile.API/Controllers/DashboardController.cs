using Microsoft.AspNetCore.Mvc;
using SmartReconcile.API.Authentication;
using SmartReconcile.Application.Abstractions;
using SmartReconcile.Application.DTOs;

namespace SmartReconcile.API.Controllers;

[ApiController]
[Route("api/dashboard")]
public sealed class DashboardController(IDashboardService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<DashboardDto>> Get(CancellationToken ct)
    {
        var tenant = ShopifyAuthenticationContext.GetTenant(HttpContext);
        return tenant is null ? Unauthorized() : Ok(await service.GetAsync(tenant.Id, ct));
    }
}
