using Microsoft.AspNetCore.Mvc;
using SmartReconcile.Application.Abstractions;

namespace SmartReconcile.API.Controllers;

[ApiController]
[Route("api/export")]
public sealed class ExportController(ICsvExporter exporter) : ControllerBase
{
    [HttpGet("csv")]
    public async Task<IActionResult> Csv(CancellationToken ct) => File(await exporter.ExportDiscrepanciesAsync(ct), "text/csv; charset=utf-8", "smartreconcile-discrepancies.csv");
}
