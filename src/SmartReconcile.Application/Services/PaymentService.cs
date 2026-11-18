using SmartReconcile.Application.Abstractions;
using SmartReconcile.Application.DTOs;
using SmartReconcile.Domain.Entities;

namespace SmartReconcile.Application.Services;

public sealed class PaymentService(IPaymentRepository repository, IOrderRepository orders) : IPaymentService
{
    public async Task<IReadOnlyList<PaymentDto>> GetAllAsync(Guid tenantId, CancellationToken cancellationToken)
        => (await repository.GetAllAsync(tenantId, cancellationToken)).Select(Map).ToList();

    public async Task<PaymentDto?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken)
        => (await repository.GetByIdAsync(id, tenantId, cancellationToken)) is { } x ? Map(x) : null;

    public async Task<PaymentDto> CreateAsync(Guid tenantId, CreatePaymentRequest request, CancellationToken cancellationToken)
    {
        var order = await orders.GetByIdAsync(request.OrderId, tenantId, cancellationToken);
        if (order is null) throw new KeyNotFoundException("Order not found.");

        var payment = Payment.Create(tenantId, request.OrderId, request.TransactionId, request.Amount, request.Status, request.PaidAtUtc ?? DateTime.UtcNow);
        await repository.AddAsync(payment, cancellationToken);
        return Map(payment);
    }

    private static PaymentDto Map(Payment x) => new(x.Id, x.OrderId, x.TransactionId, x.Amount, x.Currency, x.Status, x.PaidAtUtc);
}
