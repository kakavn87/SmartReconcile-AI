using Microsoft.AspNetCore.Mvc;
using SmartReconcile.API.Authentication;
using SmartReconcile.Application.Abstractions;
using SmartReconcile.Application.DTOs;

namespace SmartReconcile.API.Controllers;

[ApiController]
[Route("api/invoices")]
public sealed class InvoicesController(IInvoiceService service, IAiAnalysisService ai) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<IReadOnlyList<InvoiceDto>>> Get(CancellationToken ct)
    {
        var tenant = ShopifyAuthenticationContext.GetTenant(HttpContext);
        return tenant is null ? Unauthorized() : Ok(await service.GetAllAsync(tenant.Id, ct));
    }

    [HttpGet("{id:guid}")] public async Task<ActionResult<InvoiceDto>> GetById(Guid id, CancellationToken ct)
    {
        var tenant = ShopifyAuthenticationContext.GetTenant(HttpContext);
        if (tenant is null) return Unauthorized();
        return await service.GetByIdAsync(tenant.Id, id, ct) is { } x ? Ok(x) : NotFound();
    }

    [HttpPost] public async Task<ActionResult<InvoiceDto>> Create(CreateInvoiceRequest request, CancellationToken ct)
    {
        var tenant = ShopifyAuthenticationContext.GetTenant(HttpContext);
        if (tenant is null) return Unauthorized();
        var result = await service.CreateAsync(tenant.Id, request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPost("analyze")]
    public Task<AiAnalysisResponse> Analyze(AiAnalysisRequest request, CancellationToken ct) => ai.AnalyzeInvoiceAsync(request, ct);
}
