using Microsoft.EntityFrameworkCore;
using SmartReconcile.Application.Abstractions;
using SmartReconcile.Domain.Entities;
using SmartReconcile.Domain.Enums;
using SmartReconcile.Infrastructure.Persistence;

namespace SmartReconcile.Infrastructure.Repositories;

public sealed class OrderRepository(SmartReconcileDbContext db) : IOrderRepository
{
    public Task<List<Order>> GetAllAsync(Guid tenantId, CancellationToken ct) => db.Orders.AsNoTracking().Where(x => x.TenantId == tenantId).OrderByDescending(x => x.CreatedAtUtc).ToListAsync(ct);
    public Task<Order?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct) => db.Orders.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);
    public async Task AddAsync(Order order, CancellationToken ct) { await db.Orders.AddAsync(order, ct); await db.SaveChangesAsync(ct); }
    public Task<int> CountAsync(Guid tenantId, CancellationToken ct) => db.Orders.CountAsync(x => x.TenantId == tenantId, ct);
}

public sealed class PaymentRepository(SmartReconcileDbContext db) : IPaymentRepository
{
    public Task<List<Payment>> GetAllAsync(Guid tenantId, CancellationToken ct) => db.Payments.AsNoTracking().Where(x => x.TenantId == tenantId).OrderByDescending(x => x.PaidAtUtc).ToListAsync(ct);
    public Task<Payment?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct) => db.Payments.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);
    public Task<Payment?> GetForOrderAsync(Guid orderId, Guid tenantId, CancellationToken ct) => db.Payments.FirstOrDefaultAsync(x => x.OrderId == orderId && x.TenantId == tenantId, ct);
    public async Task AddAsync(Payment payment, CancellationToken ct) { await db.Payments.AddAsync(payment, ct); await db.SaveChangesAsync(ct); }
    public Task<int> CountAsync(Guid tenantId, CancellationToken ct) => db.Payments.CountAsync(x => x.TenantId == tenantId, ct);
}

public sealed class InvoiceRepository(SmartReconcileDbContext db) : IInvoiceRepository
{
    public Task<List<Invoice>> GetAllAsync(Guid tenantId, CancellationToken ct) => db.Invoices.AsNoTracking().Where(x => x.TenantId == tenantId).OrderByDescending(x => x.InvoiceDateUtc).ToListAsync(ct);
    public Task<Invoice?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct) => db.Invoices.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);
    public Task<Invoice?> GetSalesInvoiceForOrderAsync(Guid orderId, Guid tenantId, CancellationToken ct) => db.Invoices.AsNoTracking().Where(x => x.TenantId == tenantId && x.InvoiceType == InvoiceType.Sales && x.DocumentType == InvoiceDocumentType.Invoice && x.OrderId == orderId).OrderBy(x => x.InvoiceDateUtc).ThenBy(x => x.InvoiceNumber).FirstOrDefaultAsync(ct);
    public async Task AddAsync(Invoice invoice, CancellationToken ct) { await db.Invoices.AddAsync(invoice, ct); await db.SaveChangesAsync(ct); }
    public Task<int> CountAsync(Guid tenantId, CancellationToken ct) => db.Invoices.CountAsync(x => x.TenantId == tenantId, ct);
}

public sealed class DiscrepancyRepository(SmartReconcileDbContext db) : IDiscrepancyRepository
{
    public Task<List<Discrepancy>> GetAllAsync(Guid tenantId, CancellationToken ct) => db.Discrepancies.AsNoTracking().Where(x => x.TenantId == tenantId).OrderByDescending(x => x.DetectedAtUtc).ToListAsync(ct);
    public Task<Discrepancy?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct) => db.Discrepancies.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);
    public Task<bool> ExistsOpenAsync(Guid tenantId, DiscrepancyType type, Guid? orderId, Guid? paymentId, Guid? invoiceId, CancellationToken ct)
        => db.Discrepancies.AnyAsync(x => x.TenantId == tenantId && x.Type == type && x.Status == DiscrepancyStatus.Open && x.OrderId == orderId && x.PaymentId == paymentId && x.InvoiceId == invoiceId, ct);
    public async Task AddAsync(Discrepancy discrepancy, CancellationToken ct) => await db.Discrepancies.AddAsync(discrepancy, ct);
    public Task SaveAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
    public Task<int> CountOpenAsync(Guid tenantId, CancellationToken ct) => db.Discrepancies.CountAsync(x => x.TenantId == tenantId && x.Status == DiscrepancyStatus.Open, ct);
    public Task<decimal> SumOpenAffectedAmountAsync(Guid tenantId, CancellationToken ct) => db.Discrepancies.Where(x => x.TenantId == tenantId && x.Status == DiscrepancyStatus.Open).SumAsync(x => x.AffectedAmount, ct);
}
