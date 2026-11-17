using SmartReconcile.Application.Abstractions;
using SmartReconcile.Application.DTOs;
using SmartReconcile.Domain.Enums;

namespace SmartReconcile.Application.Services;

public sealed class DashboardService(IOrderRepository orders, IPaymentRepository payments, IInvoiceRepository invoices, IDiscrepancyRepository discrepancies) : IDashboardService
{
    public async Task<DashboardDto> GetAsync(CancellationToken cancellationToken)
    {
        var orderList = await orders.GetAllAsync(cancellationToken);
        return new DashboardDto(
            orderList.Where(x => x.Status != OrderStatus.Cancelled).Sum(x => x.TotalAmount),
            await discrepancies.CountOpenAsync(cancellationToken),
            await discrepancies.SumOpenAffectedAmountAsync(cancellationToken),
            await orders.CountAsync(cancellationToken),
            await payments.CountAsync(cancellationToken),
            await invoices.CountAsync(cancellationToken));
    }
}