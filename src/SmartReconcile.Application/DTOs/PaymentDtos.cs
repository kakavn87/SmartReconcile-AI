using SmartReconcile.Domain.Enums;

namespace SmartReconcile.Application.DTOs;

public sealed record CreatePaymentRequest(Guid OrderId, string TransactionId, decimal Amount, PaymentStatus Status, DateTime? PaidAtUtc);
public sealed record PaymentDto(Guid Id, Guid OrderId, string TransactionId, decimal Amount, string Currency, PaymentStatus Status, DateTime PaidAtUtc);