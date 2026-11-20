using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartReconcile.API.Authentication;
using SmartReconcile.Application.Abstractions;
using SmartReconcile.Application.DTOs;
using SmartReconcile.Infrastructure.Persistence;
using SmartReconcile.Infrastructure.Services;

namespace SmartReconcile.API.Controllers;

[ApiController]
[Route("api/invoices")]
public sealed class InvoicesController(IInvoiceService service, IAiAnalysisService ai, SmartReconcileDbContext db, InvoiceNumberService numbers, InvoicePdfService pdf) : ControllerBase
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

        if (request.InvoiceType == SmartReconcile.Domain.Enums.InvoiceType.Sales && request.OrderId.HasValue)
        {
            var order = await db.Orders
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.TenantId == tenant.Id && x.Id == request.OrderId.Value, ct);

            if (order is null)
                return NotFound("Bestellung nicht gefunden.");

            // A cancelled Shopify order must never be manually turned into a new
            // sales invoice. Cancellation is only reversed by a Stornorechnung
            // when an original sales invoice already exists.
            if (order.Status == SmartReconcile.Domain.Enums.OrderStatus.Cancelled)
                return Conflict("Für eine stornierte Bestellung kann keine neue Ausgangsrechnung erstellt werden. Prüfen Sie die vorhandene Rechnung bzw. Stornorechnung.");

            var existing = await db.Invoices
                .AsNoTracking()
                .Where(x => x.TenantId == tenant.Id
                    && x.InvoiceType == SmartReconcile.Domain.Enums.InvoiceType.Sales
                    && x.DocumentType == SmartReconcile.Domain.Enums.InvoiceDocumentType.Invoice
                    && x.OrderId == request.OrderId.Value)
                .OrderBy(x => x.InvoiceDateUtc)
                .ThenBy(x => x.InvoiceNumber)
                .FirstOrDefaultAsync(ct);

            if (existing is not null)
                return Ok(Map(existing));
        }

        if (string.IsNullOrWhiteSpace(request.InvoiceNumber) && request.InvoiceType == SmartReconcile.Domain.Enums.InvoiceType.Sales)
            request = request with { InvoiceNumber = await numbers.NextAsync(tenant.Id, DateTime.UtcNow.Year, "RE", ct) };
        if (request.InvoiceDateUtc is null)
            request = request with { InvoiceDateUtc = DateTime.UtcNow };
        InvoiceDto result;
        try
        {
            result = await service.CreateAsync(tenant.Id, request, ct);
        }
        catch (DbUpdateException) when (request.InvoiceType == SmartReconcile.Domain.Enums.InvoiceType.Sales && request.OrderId.HasValue)
        {
            var concurrent = await db.Invoices
                .AsNoTracking()
                .Where(x => x.TenantId == tenant.Id
                    && x.InvoiceType == SmartReconcile.Domain.Enums.InvoiceType.Sales
                    && x.DocumentType == SmartReconcile.Domain.Enums.InvoiceDocumentType.Invoice
                    && x.OrderId == request.OrderId.Value)
                .OrderBy(x => x.InvoiceDateUtc)
                .ThenBy(x => x.InvoiceNumber)
                .FirstOrDefaultAsync(ct);

            if (concurrent is null)
                throw;

            return Ok(Map(concurrent));
        }

        var entity = await db.Invoices.FirstAsync(x => x.Id == result.Id && x.TenantId == tenant.Id, ct);
        if (entity.PdfContent is null)
        {
            var settings = await db.AccountingSettings.FirstOrDefaultAsync(x => x.TenantId == tenant.Id, ct);
            entity.AttachPdf(pdf.Generate(entity, settings ?? SmartReconcile.Domain.Entities.AccountingSettings.CreateDefault(tenant.Id)));
            await db.SaveChangesAsync(ct);
        }
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, Map(entity));
    }

    private static InvoiceDto Map(SmartReconcile.Domain.Entities.Invoice x)
        => new(x.Id, x.InvoiceType, x.DocumentType, x.OrderId, x.OriginalInvoiceId, x.RefundId, x.ShopifyOrderId, x.InvoiceNumber, x.SupplierName, x.CustomerName, x.CustomerEmail, x.CustomerAddress, x.Amount, x.NetAmount, x.TaxRate, x.TaxType, x.TaxAmount, x.GrossAmount, x.Currency, x.InvoiceDateUtc, x.PdfGeneratedAtUtc, x.RawText);

    [HttpPost("analyze")]
    public Task<AiAnalysisResponse> Analyze(AiAnalysisRequest request, CancellationToken ct) => ai.AnalyzeInvoiceAsync(request, ct);
}
