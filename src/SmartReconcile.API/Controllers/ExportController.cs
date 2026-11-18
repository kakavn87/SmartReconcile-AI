using Microsoft.AspNetCore.Mvc;
using SmartReconcile.API.Authentication;
using SmartReconcile.Application.Abstractions;

namespace SmartReconcile.API.Controllers;

[ApiController]
[Route("api/export")]
public sealed class ExportController(ICsvExporter exporter, IDatevExtfExporter datevExporter) : ControllerBase
{
    [HttpGet("csv")]
    public async Task<IActionResult> Csv(CancellationToken cancellationToken)
    {
        var tenant = ShopifyAuthenticationContext.GetTenant(HttpContext);
        if (tenant is null) return Unauthorized();
        return File(await exporter.ExportDiscrepanciesAsync(tenant.Id, cancellationToken), "text/csv; charset=utf-8", "smartreconcile-discrepancies.csv");
    }

    [HttpGet("datev")]
    public async Task<IActionResult> Datev([FromQuery] DateOnly from, [FromQuery] DateOnly to, [FromQuery] bool onlyReconciled = true, CancellationToken cancellationToken = default)
    {
        var tenant = ShopifyAuthenticationContext.GetTenant(HttpContext);
        if (tenant is null) return Unauthorized();
        try
        {
            var bytes = await datevExporter.ExportAsync(tenant.Id, from, to, onlyReconciled, cancellationToken);
            return File(bytes, "text/csv; charset=windows-1252", $"EXTF_Buchungsstapel_{from:yyyyMMdd}_{to:yyyyMMdd}.csv");
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }
}
