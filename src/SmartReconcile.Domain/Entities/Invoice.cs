namespace SmartReconcile.Domain.Entities;

public sealed class Invoice
{
    private Invoice() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string InvoiceNumber { get; private set; } = string.Empty;
    public string SupplierName { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "EUR";
    public DateTime InvoiceDateUtc { get; private set; }
    public string RawText { get; private set; } = string.Empty;

    public static Invoice Create(Guid tenantId, string invoiceNumber, string supplierName, decimal amount, DateTime invoiceDateUtc, string rawText)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant id is required.", nameof(tenantId));

        return new Invoice
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            InvoiceNumber = invoiceNumber.Trim(),
            SupplierName = supplierName.Trim(),
            Amount = decimal.Round(amount, 2),
            Currency = "EUR",
            InvoiceDateUtc = invoiceDateUtc,
            RawText = rawText ?? string.Empty
        };
    }
}
