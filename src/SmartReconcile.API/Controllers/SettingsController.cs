using Microsoft.AspNetCore.Mvc;
using SmartReconcile.API.Authentication;
using SmartReconcile.Application.Abstractions;
using SmartReconcile.Application.DTOs;

namespace SmartReconcile.API.Controllers;

[ApiController]
[Route("api/settings")]
public sealed class SettingsController(IAccountingSettingsService accountingSettings) : ControllerBase
{
    [HttpGet("accounting")]
    public async Task<ActionResult<AccountingSettingsDto>> GetAccounting(CancellationToken ct)
    {
        var tenant = ShopifyAuthenticationContext.GetTenant(HttpContext);
        return tenant is null ? Unauthorized() : Ok(await accountingSettings.GetAsync(tenant.Id, ct));
    }

    [HttpPut("accounting")]
    public async Task<ActionResult<AccountingSettingsDto>> UpdateAccounting(UpdateAccountingSettingsRequest request, CancellationToken ct)
    {
        var tenant = ShopifyAuthenticationContext.GetTenant(HttpContext);
        if (tenant is null) return Unauthorized();
        try { return Ok(await accountingSettings.UpdateAsync(tenant.Id, request, ct)); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }
}
