using Microsoft.AspNetCore.Mvc;
using SmartReconcile.Application.Abstractions;
using SmartReconcile.Application.DTOs;

namespace SmartReconcile.API.Controllers;

[ApiController]
[Route("api/reconciliation")]
public sealed class ReconciliationController(IReconciliationService service) : ControllerBase
{
    [HttpPost("run")]
    public async Task<ActionResult<ReconciliationResult>> Run(CancellationToken ct) => Ok(await service.ReconcileAsync(ct));
}