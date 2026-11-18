using Microsoft.EntityFrameworkCore;
using SmartReconcile.Application.Abstractions;
using SmartReconcile.Domain.Entities;
using SmartReconcile.Domain.Enums;
using SmartReconcile.Infrastructure.Persistence;

namespace SmartReconcile.Infrastructure.Repositories;

public sealed class OrderRepository(SmartReconcileDbContext db) : IOrderRepository
{
    public Task<List<Order>> GetAllAsync(CancellationToken ct) => db.Orders.AsNoTracking().OrderByDescending(x => x.CreatedAtUtc).ToListAsync(ct);
    public Task<List<Order>> GetAllAsync(Guid tenantId, CancellationToken ct) => db.Orders.AsNoTracking()
        .Where(x => x.TenantId == tenantId)
        .OrderByDescending(x => x.CreatedAtUtc)
        .ToListAsync(ct);
    public Task<Order?> GetByIdAsync(Guid id, CancellationToken ct) => db.Orders.FirstOrDefaultAsync(x => x.Id == id, ct);
    public Task<Order?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct) => db.Orders.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);
    public async Task AddAsync(Order order, CancellationToken ct) { await db.Orders.AddAsync(order, ct); await db.SaveChangesAsync(ct); }
    public Task<int> CountAsync(CancellationToken ct) => db.Orders.CountAsync(ct);
}

public sealed class PaymentRepository(SmartReconcileDbContext db) : IPaymentRepository
{
    public Task<List<Payment>> GetAllAsync(CancellationToken ct) => db.Payments.AsNoTracking().OrderByDescending(x => x.PaidAtUtc).ToListAsync(ct);
    public Task<Payment?> GetByIdAsync(Guid id, CancellationToken ct) => db.Payments.FirstOrDefaultAsync(x => x.Id == id, ct);
    public Task<Payment?> GetForOrderAsync(Guid orderId, CancellationToken ct) => db.Payments.FirstOrDefaultAsync(x => x.OrderId == orderId, ct);
    public async Task AddAsync(Payment payment, CancellationToken ct) { await db.Payments.AddAsync(payment, ct); await db.SaveChangesAsync(ct); }
    public Task<int> CountAsync(CancellationToken ct) => db.Payments.CountAsync(ct);
}

public sealed class InvoiceRepository(SmartReconcileDbContext db) : IInvoiceRepository
{
    public Task<List<Invoice>> GetAllAsync(CancellationToken ct) => db.Invoices.AsNoTracking().OrderByDescending(x => x.InvoiceDateUtc).ToListAsync(ct);
    public Task<Invoice?> GetByIdAsync(Guid id, CancellationToken ct) => db.Invoices.FirstOrDefaultAsync(x => x.Id == id, ct);
    public async Task AddAsync(Invoice invoice, CancellationToken ct) { await db.Invoices.AddAsync(invoice, ct); await db.SaveChangesAsync(ct); }
    public Task<int> CountAsync(CancellationToken ct) => db.Invoices.CountAsync(ct);
}

public sealed class DiscrepancyRepository(SmartReconcileDbContext db) : IDiscrepancyRepository
{
    public Task<List<Discrepancy>> GetAllAsync(CancellationToken ct) => db.Discrepancies.AsNoTracking().OrderByDescending(x => x.DetectedAtUtc).ToListAsync(ct);
    public Task<Discrepancy?> GetByIdAsync(Guid id, CancellationToken ct) => db.Discrepancies.FirstOrDefaultAsync(x => x.Id == id, ct);
    public Task<bool> ExistsOpenAsync(DiscrepancyType type, Guid? orderId, Guid? paymentId, Guid? invoiceId, CancellationToken ct)
        => db.Discrepancies.AnyAsync(x => x.Type == type && x.Status == DiscrepancyStatus.Open && x.OrderId == orderId && x.PaymentId == paymentId && x.InvoiceId == invoiceId, ct);
    public async Task AddAsync(Discrepancy discrepancy, CancellationToken ct) => await db.Discrepancies.AddAsync(discrepancy, ct);
    public Task SaveAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
    public Task<int> CountOpenAsync(CancellationToken ct) => db.Discrepancies.CountAsync(x => x.Status == DiscrepancyStatus.Open, ct);
    public Task<decimal> SumOpenAffectedAmountAsync(CancellationToken ct) => db.Discrepancies.Where(x => x.Status == DiscrepancyStatus.Open).SumAsync(x => x.AffectedAmount, ct);
}