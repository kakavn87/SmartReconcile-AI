using SmartReconcile.Domain.Enums;

namespace SmartReconcile.Application.DTOs;

public sealed record CreateInvoiceRequest(
    InvoiceType InvoiceType,
    string InvoiceNumber,
    string PartnerName,
    decimal Amount,
    string Currency,
    DateTime? InvoiceDateUtc,
    string RawText,
    Guid? OrderId);

public sealed record InvoiceDto(
    Guid Id,
    InvoiceType InvoiceType,
    InvoiceDocumentType DocumentType,
    Guid? OrderId,
    Guid? OriginalInvoiceId,
    string? RefundId,
    string? ShopifyOrderId,
    string InvoiceNumber,
    string SupplierName,
    string? CustomerName,
    string? CustomerEmail,
    string? CustomerAddress,
    decimal Amount,
    decimal NetAmount,
    decimal TaxRate,
    InvoiceTaxType TaxType,
    decimal TaxAmount,
    decimal GrossAmount,
    string Currency,
    DateTime InvoiceDateUtc,
    DateTime? PdfGeneratedAtUtc,
    string RawText);
