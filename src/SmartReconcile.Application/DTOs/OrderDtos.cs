using SmartReconcile.Domain.Enums;

namespace SmartReconcile.Application.DTOs;

public sealed record CreateOrderRequest(string OrderNumber, string CustomerName, string CustomerEmail, decimal TotalAmount, OrderStatus Status, DateTime? CreatedAtUtc);
public sealed record OrderDto(Guid Id, string OrderNumber, string CustomerName, string CustomerEmail, decimal TotalAmount, string Currency, OrderStatus Status, DateTime CreatedAtUtc);