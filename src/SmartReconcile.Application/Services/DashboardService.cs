using SmartReconcile.Application.Abstractions;
using SmartReconcile.Application.DTOs;
using SmartReconcile.Domain.Enums;

namespace SmartReconcile.Application.Services;

public sealed class DashboardService(IOrderRepository orders, IPaymentRepository payments, IInvoiceRepository invoices, IDiscrepancyRepository discrepancies) : IDashboardService
{
    public async Task<DashboardDto> GetAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var orderList = await orders.GetAllAsync(tenantId, cancellationToken);
        return new DashboardDto(
            orderList.Where(x => x.Status != OrderStatus.Cancelled).Sum(x => x.TotalAmount),
            await discrepancies.CountOpenAsync(tenantId, cancellationToken),
            await discrepancies.SumOpenAffectedAmountAsync(tenantId, cancellationToken),
            await orders.CountAsync(tenantId, cancellationToken),
            await payments.CountAsync(tenantId, cancellationToken),
            await invoices.CountAsync(tenantId, cancellationToken));
    }
}
