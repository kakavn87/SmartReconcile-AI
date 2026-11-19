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
    Guid? OrderId,
    string InvoiceNumber,
    string SupplierName,
    string? CustomerName,
    decimal Amount,
    string Currency,
    DateTime InvoiceDateUtc,
    string RawText);
