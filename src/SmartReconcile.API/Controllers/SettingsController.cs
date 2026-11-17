using Microsoft.AspNetCore.Mvc;
using SmartReconcile.Application.Abstractions;
using SmartReconcile.Application.DTOs;

namespace SmartReconcile.API.Controllers;

[ApiController]
[Route("api/settings")]
public sealed class SettingsController(IAccountingSettingsService accountingSettings) : ControllerBase
{
    [HttpGet("accounting")]
    public Task<AccountingSettingsDto> GetAccounting(CancellationToken cancellationToken)
        => accountingSettings.GetAsync(cancellationToken);

    [HttpPut("accounting")]
    public async Task<ActionResult<AccountingSettingsDto>> UpdateAccounting(
        [FromBody] UpdateAccountingSettingsRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await accountingSettings.UpdateAsync(request, cancellationToken));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
