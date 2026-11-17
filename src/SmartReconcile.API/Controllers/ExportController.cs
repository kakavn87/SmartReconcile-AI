using Microsoft.AspNetCore.Mvc;
using SmartReconcile.Application.Abstractions;

namespace SmartReconcile.API.Controllers;

[ApiController]
[Route("api/export")]
public sealed class ExportController(
    ICsvExporter exporter,
    IDatevExtfExporter datevExporter) : ControllerBase
{
    [HttpGet("csv")]
    public async Task<IActionResult> Csv(CancellationToken cancellationToken)
        => File(
            await exporter.ExportDiscrepanciesAsync(cancellationToken),
            "text/csv; charset=utf-8",
            "smartreconcile-discrepancies.csv");

    [HttpGet("datev")]
    public async Task<IActionResult> Datev(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        [FromQuery] bool onlyReconciled = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var bytes = await datevExporter.ExportAsync(from, to, onlyReconciled, cancellationToken);
            return File(
                bytes,
                "text/csv; charset=windows-1252",
                $"EXTF_Buchungsstapel_{from:yyyyMMdd}_{to:yyyyMMdd}.csv");
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
