using Microsoft.AspNetCore.Mvc;
using SmartReconcile.Application.Abstractions;
using SmartReconcile.Application.DTOs;
using SmartReconcile.Domain.Enums;

namespace SmartReconcile.API.Controllers;

[ApiController]
[Route("api/control-tower")]
public sealed class ControlTowerController(IControlTowerService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ControlTowerDto>> Get(CancellationToken cancellationToken)
        => Ok(await service.GetOverviewAsync(cancellationToken));

    [HttpGet("cases/{id:guid}")]
    public async Task<ActionResult<FinancialCaseDetailDto>> GetCase(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.GetCaseDetailAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("cases/{id:guid}/investigate")]
    public async Task<ActionResult<FinancialCaseDto>> Investigate(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.StartInvestigationAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("cases/{id:guid}/auto-fix")]
    public async Task<ActionResult<FinancialCaseDto>> AutoFix(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.AutoFixAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("cases/{id:guid}/resolve")]
    public async Task<ActionResult<FinancialCaseDto>> Resolve(
        Guid id,
        [FromBody] ResolveFinancialCaseRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.ResolveAsync(id, request.RootCause, request.Note, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("cases/{id:guid}/ignore")]
    public async Task<ActionResult<FinancialCaseDto>> Ignore(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.IgnoreAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("graph/{nodeType}/{entityId:guid}")]
    public async Task<ActionResult<List<FinancialGraphRelationDto>>> Graph(
        FinancialNodeType nodeType,
        Guid entityId,
        CancellationToken cancellationToken)
        => Ok(await service.GetGraphAsync(nodeType, entityId, cancellationToken));
}

public sealed record ResolveFinancialCaseRequest(string? RootCause, string? Note);
