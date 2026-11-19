using SmartReconcile.Application.Abstractions;
using SmartReconcile.Application.DTOs;
using SmartReconcile.Domain.Entities;
using SmartReconcile.Domain.Enums;

namespace SmartReconcile.Application.Services;

public sealed class OrderService(IOrderRepository repository, IInvoiceRepository invoices) : IOrderService
{
    public async Task<IReadOnlyList<OrderDto>> GetAllAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var orders = await repository.GetAllAsync(tenantId, cancellationToken);
        var salesInvoices = await invoices.GetAllAsync(tenantId, cancellationToken);
        return orders.Select(x => Map(x, salesInvoices.FirstOrDefault(i => i.InvoiceType == InvoiceType.Sales && i.DocumentType == InvoiceDocumentType.Invoice && i.OrderId == x.Id))).ToList();
    }

    public async Task<OrderDto?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken)
    {
        var order = await repository.GetByIdAsync(id, tenantId, cancellationToken);
        if (order is null) return null;
        var invoice = await invoices.GetSalesInvoiceForOrderAsync(id, tenantId, cancellationToken);
        return Map(order, invoice);
    }

    public async Task<OrderDto> CreateAsync(Guid tenantId, CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var order = Order.Create(request.OrderNumber, request.CustomerName, request.CustomerEmail, request.TotalAmount, request.Status, request.CreatedAtUtc ?? DateTime.UtcNow, tenantId: tenantId);
        await repository.AddAsync(order, cancellationToken);
        return Map(order, null);
    }

    private static OrderDto Map(Order x, Invoice? invoice) => new(
        x.Id, x.OrderNumber, x.CustomerName, x.CustomerEmail, x.TotalAmount,
        string.IsNullOrWhiteSpace(x.Currency) ? "EUR" : x.Currency,
        x.Status.ToString(), x.CreatedAtUtc,
        invoice?.Id, invoice?.InvoiceNumber);
}
