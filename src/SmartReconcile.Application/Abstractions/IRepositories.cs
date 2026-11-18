using SmartReconcile.Domain.Entities;
using SmartReconcile.Domain.Enums;

namespace SmartReconcile.Application.Abstractions;

public interface IOrderRepository
{
    Task<List<Order>> GetAllAsync(Guid tenantId, CancellationToken cancellationToken);
    Task<Order?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken cancellationToken);
    Task AddAsync(Order order, CancellationToken cancellationToken);
    Task<int> CountAsync(Guid tenantId, CancellationToken cancellationToken);
}

public interface IPaymentRepository
{
    Task<List<Payment>> GetAllAsync(Guid tenantId, CancellationToken cancellationToken);
    Task<Payment?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken cancellationToken);
    Task<Payment?> GetForOrderAsync(Guid orderId, Guid tenantId, CancellationToken cancellationToken);
    Task AddAsync(Payment payment, CancellationToken cancellationToken);
    Task<int> CountAsync(Guid tenantId, CancellationToken cancellationToken);
}

public interface IInvoiceRepository
{
    Task<List<Invoice>> GetAllAsync(Guid tenantId, CancellationToken cancellationToken);
    Task<Invoice?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken cancellationToken);
    Task AddAsync(Invoice invoice, CancellationToken cancellationToken);
    Task<int> CountAsync(Guid tenantId, CancellationToken cancellationToken);
}

public interface IDiscrepancyRepository
{
    Task<List<Discrepancy>> GetAllAsync(Guid tenantId, CancellationToken cancellationToken);
    Task<Discrepancy?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken cancellationToken);
    Task<bool> ExistsOpenAsync(Guid tenantId, DiscrepancyType type, Guid? orderId, Guid? paymentId, Guid? invoiceId, CancellationToken cancellationToken);
    Task AddAsync(Discrepancy discrepancy, CancellationToken cancellationToken);
    Task SaveAsync(CancellationToken cancellationToken);
    Task<int> CountOpenAsync(Guid tenantId, CancellationToken cancellationToken);
    Task<decimal> SumOpenAffectedAmountAsync(Guid tenantId, CancellationToken cancellationToken);
}
