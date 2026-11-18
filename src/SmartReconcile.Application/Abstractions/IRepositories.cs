using SmartReconcile.Domain.Entities;
using SmartReconcile.Domain.Enums;

namespace SmartReconcile.Application.Abstractions;

public interface IOrderRepository
{
    Task<List<Order>> GetAllAsync(Guid tenantId, CancellationToken cancellationToken);
    Task<Order?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken cancellationToken);
    Task AddAsync(Order order, CancellationToken cancellationToken);
    Task<int> CountAsync(CancellationToken cancellationToken);
}

public interface IPaymentRepository
{
    Task<List<Payment>> GetAllAsync(CancellationToken cancellationToken);
    Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Payment?> GetForOrderAsync(Guid orderId, CancellationToken cancellationToken);
    Task AddAsync(Payment payment, CancellationToken cancellationToken);
    Task<int> CountAsync(CancellationToken cancellationToken);
}

public interface IInvoiceRepository
{
    Task<List<Invoice>> GetAllAsync(CancellationToken cancellationToken);
    Task<Invoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(Invoice invoice, CancellationToken cancellationToken);
    Task<int> CountAsync(CancellationToken cancellationToken);
}

public interface IDiscrepancyRepository
{
    Task<List<Discrepancy>> GetAllAsync(CancellationToken cancellationToken);
    Task<Discrepancy?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> ExistsOpenAsync(DiscrepancyType type, Guid? orderId, Guid? paymentId, Guid? invoiceId, CancellationToken cancellationToken);
    Task AddAsync(Discrepancy discrepancy, CancellationToken cancellationToken);
    Task SaveAsync(CancellationToken cancellationToken);
    Task<int> CountOpenAsync(CancellationToken cancellationToken);
    Task<decimal> SumOpenAffectedAmountAsync(CancellationToken cancellationToken);
}