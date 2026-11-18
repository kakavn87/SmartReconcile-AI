using SmartReconcile.Application.Abstractions;
using SmartReconcile.Application.DTOs;
using SmartReconcile.Domain.Entities;

namespace SmartReconcile.Application.Services;

public sealed class OrderService(IOrderRepository repository) : IOrderService
{
    public async Task<IReadOnlyList<OrderDto>> GetAllAsync(Guid tenantId, CancellationToken cancellationToken)
        => (await repository.GetAllAsync(tenantId, cancellationToken)).Select(Map).ToList();

    public async Task<OrderDto?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken)
        => (await repository.GetByIdAsync(id, tenantId, cancellationToken)) is { } x ? Map(x) : null;

    public async Task<OrderDto> CreateAsync(Guid tenantId, CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var order = Order.Create(request.OrderNumber, request.CustomerName, request.CustomerEmail, request.TotalAmount, request.Status, request.CreatedAtUtc ?? DateTime.UtcNow, tenantId: tenantId);
        await repository.AddAsync(order, cancellationToken);
        return Map(order);
    }

    private static OrderDto Map(Order x) => new(
        x.Id, x.OrderNumber, x.CustomerName, x.CustomerEmail, x.TotalAmount,
        string.IsNullOrWhiteSpace(x.Currency) ? "EUR" : x.Currency,
        x.Status.ToString(), x.CreatedAtUtc);
}
