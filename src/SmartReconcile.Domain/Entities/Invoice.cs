using SmartReconcile.Domain.Enums;

namespace SmartReconcile.Domain.Entities;

public sealed class Invoice
{
    private Invoice() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public InvoiceType InvoiceType { get; private set; }
    public InvoiceDocumentType DocumentType { get; private set; }
    public Guid? OrderId { get; private set; }
    public Guid? OriginalInvoiceId { get; private set; }
    public string? RefundId { get; private set; }
    public string? ShopifyOrderId { get; private set; }
    public string InvoiceNumber { get; private set; } = string.Empty;
    public string SupplierName { get; private set; } = string.Empty;
    public string? CustomerName { get; private set; }
    public string? CustomerEmail { get; private set; }
    public string? CustomerAddress { get; private set; }
    public decimal Amount { get; private set; }
    public decimal NetAmount { get; private set; }
    public decimal TaxRate { get; private set; }
    public InvoiceTaxType TaxType { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal GrossAmount { get; private set; }
    public string Currency { get; private set; } = "EUR";
    public DateTime InvoiceDateUtc { get; private set; }
    public string RawText { get; private set; } = string.Empty;
    public byte[]? PdfContent { get; private set; }
    public DateTime? PdfGeneratedAtUtc { get; private set; }

    public static Invoice Create(
        Guid tenantId,
        InvoiceType invoiceType,
        string invoiceNumber,
        string partnerName,
        decimal amount,
        string currency,
        DateTime invoiceDateUtc,
        string rawText,
        Guid? orderId = null,
        decimal taxRate = 19m,
        InvoiceTaxType taxType = InvoiceTaxType.Standard,
        InvoiceDocumentType documentType = InvoiceDocumentType.Invoice,
        Guid? originalInvoiceId = null,
        string? refundId = null,
        string? shopifyOrderId = null,
        string? customerEmail = null,
        string? customerAddress = null)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant id is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(invoiceNumber)) throw new ArgumentException("Invoice number is required.", nameof(invoiceNumber));
        if (invoiceType == InvoiceType.Sales && orderId == Guid.Empty) orderId = null;
        if (documentType == InvoiceDocumentType.Storno && originalInvoiceId is null)
            throw new ArgumentException("A Stornorechnung requires the original invoice.", nameof(originalInvoiceId));
        if (taxRate < 0m || taxRate > 100m) throw new ArgumentOutOfRangeException(nameof(taxRate));

        var gross = decimal.Round(Math.Abs(amount), 2);
        var net = taxRate == 0m ? gross : decimal.Round(gross / (1m + taxRate / 100m), 2);
        var tax = decimal.Round(gross - net, 2);

        return new Invoice
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            InvoiceType = invoiceType,
            DocumentType = documentType,
            OrderId = invoiceType == InvoiceType.Sales ? orderId : null,
            OriginalInvoiceId = originalInvoiceId,
            RefundId = refundId,
            ShopifyOrderId = shopifyOrderId,
            InvoiceNumber = invoiceNumber.Trim(),
            SupplierName = invoiceType == InvoiceType.Purchase ? partnerName.Trim() : string.Empty,
            CustomerName = invoiceType == InvoiceType.Sales ? partnerName.Trim() : null,
            CustomerEmail = invoiceType == InvoiceType.Sales ? customerEmail?.Trim() : null,
            CustomerAddress = invoiceType == InvoiceType.Sales ? customerAddress?.Trim() : null,
            Amount = gross,
            NetAmount = net,
            TaxRate = taxRate,
            TaxType = taxType,
            TaxAmount = tax,
            GrossAmount = gross,
            Currency = string.IsNullOrWhiteSpace(currency) ? "EUR" : currency.Trim().ToUpperInvariant(),
            InvoiceDateUtc = invoiceDateUtc,
            RawText = rawText ?? string.Empty
        };
    }

    public void AttachPdf(byte[] pdf)
    {
        if (PdfContent is not null)
            throw new InvalidOperationException("The invoice PDF is immutable and has already been generated.");
        if (pdf is null || pdf.Length == 0)
            throw new ArgumentException("PDF content is required.", nameof(pdf));

        PdfContent = pdf;
        PdfGeneratedAtUtc = DateTime.UtcNow;
    }
}
