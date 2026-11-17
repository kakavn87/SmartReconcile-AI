using Microsoft.AspNetCore.Mvc;
using SmartReconcile.Application.Abstractions;
using SmartReconcile.Application.DTOs;

namespace SmartReconcile.API.Controllers;

[ApiController]
[Route("api/invoices")]
public sealed class InvoicesController(IInvoiceService service, IAiAnalysisService ai) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<IReadOnlyList<InvoiceDto>>> Get(CancellationToken ct) => Ok(await service.GetAllAsync(ct));
    [HttpGet("{id:guid}")] public async Task<ActionResult<InvoiceDto>> GetById(Guid id, CancellationToken ct) => await service.GetByIdAsync(id, ct) is { } x ? Ok(x) : NotFound();
    [HttpPost] public async Task<ActionResult<InvoiceDto>> Create(CreateInvoiceRequest request, CancellationToken ct) { var result = await service.CreateAsync(request, ct); return CreatedAtAction(nameof(GetById), new { id = result.Id }, result); }
    [HttpPost("analyze")]
    public Task<AiAnalysisResponse> Analyze(AiAnalysisRequest request, CancellationToken ct) => ai.AnalyzeInvoiceAsync(request, ct);
}
