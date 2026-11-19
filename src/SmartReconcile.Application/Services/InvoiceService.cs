using SmartReconcile.Application.Abstractions;
using SmartReconcile.Application.DTOs;
using SmartReconcile.Domain.Entities;

namespace SmartReconcile.Application.Services;

public sealed class InvoiceService(
    IInvoiceRepository repository,
    IReconciliationService reconciliation) : IInvoiceService
{
    public async Task<IReadOnlyList<InvoiceDto>> GetAllAsync(Guid tenantId, CancellationToken cancellationToken)
        => (await repository.GetAllAsync(tenantId, cancellationToken)).Select(Map).ToList();

    public async Task<InvoiceDto?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken)
        => (await repository.GetByIdAsync(id, tenantId, cancellationToken)) is { } x ? Map(x) : null;

    public async Task<InvoiceDto> CreateAsync(Guid tenantId, CreateInvoiceRequest request, CancellationToken cancellationToken)
    {
        var invoice = Invoice.Create(
            tenantId,
            request.InvoiceType,
            request.InvoiceNumber,
            request.PartnerName,
            request.Amount,
            request.Currency,
            request.InvoiceDateUtc ?? DateTime.UtcNow,
            request.RawText,
            request.OrderId);

        await repository.AddAsync(invoice, cancellationToken);
        await reconciliation.ReconcileAsync(tenantId, cancellationToken);
        return Map(invoice);
    }

    private static InvoiceDto Map(Invoice x)
        => new(x.Id, x.InvoiceType, x.DocumentType, x.OrderId, x.OriginalInvoiceId, x.RefundId, x.ShopifyOrderId, x.InvoiceNumber, x.SupplierName, x.CustomerName, x.CustomerEmail, x.CustomerAddress, x.Amount, x.NetAmount, x.TaxRate, x.TaxType, x.TaxAmount, x.GrossAmount, x.Currency, x.InvoiceDateUtc, x.PdfGeneratedAtUtc, x.RawText);
}
