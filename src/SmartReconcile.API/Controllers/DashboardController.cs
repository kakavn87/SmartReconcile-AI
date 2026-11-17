using Microsoft.AspNetCore.Mvc;
using SmartReconcile.Application.Abstractions;
using SmartReconcile.Application.DTOs;

namespace SmartReconcile.API.Controllers;

[ApiController]
[Route("api/dashboard")]
public sealed class DashboardController(IDashboardService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<DashboardDto>> Get(CancellationToken ct) => Ok(await service.GetAsync(ct));
}
