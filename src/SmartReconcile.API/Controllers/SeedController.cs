using Microsoft.AspNetCore.Mvc;
using SmartReconcile.Application.Abstractions;
using SmartReconcile.Application.DTOs;

namespace SmartReconcile.API.Controllers;

[ApiController]
[Route("api/seed")]
public sealed class SeedController(ISeedService seed, IReconciliationService reconciliation) : ControllerBase
{
    [HttpPost("generate")]
    public async Task<ActionResult<SeedResult>> Generate(CancellationToken ct)
    {
        var result = await seed.GenerateAsync(ct);
        await reconciliation.ReconcileAsync(ct);
        return Ok(result);
    }
}