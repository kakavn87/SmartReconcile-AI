using System.Text.Json.Serialization;
using SmartReconcile.Domain.Enums;

namespace SmartReconcile.Application.DTOs;

public sealed record CreateOrderRequest(string OrderNumber, string CustomerName, string CustomerEmail, decimal TotalAmount, OrderStatus Status, DateTime? CreatedAtUtc);
public sealed record OrderInvoiceLinkDto(Guid Id, string InvoiceNumber, decimal Amount, string Currency);

public sealed record OrderDto(
    Guid Id,
    string OrderNumber,
    string CustomerName,
    string CustomerEmail,
    [property: JsonPropertyName("totalAmount")] decimal TotalAmount,
    [property: JsonPropertyName("currency")] string Currency,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("createdAtUtc")] DateTime CreatedAtUtc,
    [property: JsonPropertyName("invoiceId")] Guid? InvoiceId,
    [property: JsonPropertyName("invoiceNumber")] string? InvoiceNumber,
    [property: JsonPropertyName("stornoInvoices")] IReadOnlyList<OrderInvoiceLinkDto> StornoInvoices);