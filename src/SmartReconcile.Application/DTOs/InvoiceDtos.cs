namespace SmartReconcile.Application.DTOs;

public sealed record CreateInvoiceRequest(string InvoiceNumber, string SupplierName, decimal Amount, DateTime? InvoiceDateUtc, string RawText);
public sealed record InvoiceDto(Guid Id, string InvoiceNumber, string SupplierName, decimal Amount, string Currency, DateTime InvoiceDateUtc, string RawText);