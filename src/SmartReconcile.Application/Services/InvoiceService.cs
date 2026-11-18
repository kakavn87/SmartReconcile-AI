using SmartReconcile.Application.Abstractions;
using SmartReconcile.Application.DTOs;
using SmartReconcile.Domain.Entities;

namespace SmartReconcile.Application.Services;

public sealed class InvoiceService(IInvoiceRepository repository) : IInvoiceService
{
    public async Task<IReadOnlyList<InvoiceDto>> GetAllAsync(Guid tenantId, CancellationToken cancellationToken)
        => (await repository.GetAllAsync(tenantId, cancellationToken)).Select(Map).ToList();

    public async Task<InvoiceDto?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken)
        => (await repository.GetByIdAsync(id, tenantId, cancellationToken)) is { } x ? Map(x) : null;

    public async Task<InvoiceDto> CreateAsync(Guid tenantId, CreateInvoiceRequest request, CancellationToken cancellationToken)
    {
        var invoice = Invoice.Create(tenantId, request.InvoiceNumber, request.SupplierName, request.Amount, request.InvoiceDateUtc ?? DateTime.UtcNow, request.RawText);
        await repository.AddAsync(invoice, cancellationToken);
        return Map(invoice);
    }

    private static InvoiceDto Map(Invoice x) => new(x.Id, x.InvoiceNumber, x.SupplierName, x.Amount, x.Currency, x.InvoiceDateUtc, x.RawText);
}
