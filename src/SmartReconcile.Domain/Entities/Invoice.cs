using SmartReconcile.Domain.Enums;

namespace SmartReconcile.Domain.Entities;

public sealed class Invoice
{
    private Invoice() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public InvoiceType InvoiceType { get; private set; }
    public Guid? OrderId { get; private set; }
    public string InvoiceNumber { get; private set; } = string.Empty;
    public string SupplierName { get; private set; } = string.Empty;
    public string? CustomerName { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "EUR";
    public DateTime InvoiceDateUtc { get; private set; }
    public string RawText { get; private set; } = string.Empty;

    public static Invoice Create(
        Guid tenantId,
        InvoiceType invoiceType,
        string invoiceNumber,
        string partnerName,
        decimal amount,
        string currency,
        DateTime invoiceDateUtc,
        string rawText,
        Guid? orderId = null)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant id is required.", nameof(tenantId));
        if (invoiceType == InvoiceType.Sales && orderId == Guid.Empty) orderId = null;

        return new Invoice
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            InvoiceType = invoiceType,
            OrderId = invoiceType == InvoiceType.Sales ? orderId : null,
            InvoiceNumber = invoiceNumber.Trim(),
            SupplierName = invoiceType == InvoiceType.Purchase ? partnerName.Trim() : string.Empty,
            CustomerName = invoiceType == InvoiceType.Sales ? partnerName.Trim() : null,
            Amount = decimal.Round(amount, 2),
            Currency = string.IsNullOrWhiteSpace(currency) ? "EUR" : currency.Trim().ToUpperInvariant(),
            InvoiceDateUtc = invoiceDateUtc,
            RawText = rawText ?? string.Empty
        };
    }
}
