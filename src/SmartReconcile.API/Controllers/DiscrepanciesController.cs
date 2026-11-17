using Microsoft.AspNetCore.Mvc;
using SmartReconcile.Application.Abstractions;
using SmartReconcile.Application.DTOs;

namespace SmartReconcile.API.Controllers;

[ApiController]
[Route("api/discrepancies")]
public sealed class DiscrepanciesController(IDiscrepancyService service) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<IReadOnlyList<DiscrepancyDto>>> Get(CancellationToken ct) => Ok(await service.GetAllAsync(ct));
    [HttpGet("{id:guid}")] public async Task<ActionResult<DiscrepancyDto>> GetById(Guid id, CancellationToken ct) => await service.GetByIdAsync(id, ct) is { } x ? Ok(x) : NotFound();
    [HttpPatch("{id:guid}/status")] public async Task<ActionResult<DiscrepancyDto>> UpdateStatus(Guid id, UpdateDiscrepancyStatusRequest request, CancellationToken ct) => await service.UpdateStatusAsync(id, request, ct) is { } x ? Ok(x) : NotFound();
}
