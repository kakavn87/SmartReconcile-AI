using SmartReconcile.Application.Abstractions;
using SmartReconcile.Application.DTOs;
using SmartReconcile.Domain.Entities;
using SmartReconcile.Domain.Enums;

namespace SmartReconcile.Application.Services;

public sealed class ReconciliationService(
    IOrderRepository orders,
    IPaymentRepository payments,
    IInvoiceRepository invoices,
    IDiscrepancyRepository discrepancies) : IReconciliationService
{
    public async Task<ReconciliationResult> ReconcileAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var orderList = await orders.GetAllAsync(tenantId, cancellationToken);
        var paymentList = await payments.GetAllAsync(tenantId, cancellationToken);
        var invoiceList = await invoices.GetAllAsync(tenantId, cancellationToken);
        var created = new List<Discrepancy>();
        var now = DateTime.UtcNow;

        foreach (var order in orderList)
        {
            var payment = paymentList.FirstOrDefault(p => p.OrderId == order.Id && p.TenantId == tenantId);
            if (order.Status == OrderStatus.Pending && now - order.CreatedAtUtc > TimeSpan.FromHours(48) && (payment is null || payment.Status != PaymentStatus.Succeeded))
                await AddIfMissing(tenantId, DiscrepancyType.MissingPayment, "Zahlung fehlt", $"Bestellung {order.OrderNumber} ist seit mehr als 48 Stunden offen und hat keine erfolgreiche Zahlung.", order.TotalAmount, order.Id, payment?.Id, null, created, cancellationToken);

            if (payment is not null && payment.Status == PaymentStatus.Succeeded && (order.Status == OrderStatus.Pending || order.Status == OrderStatus.OnHold))
                await AddIfMissing(tenantId, DiscrepancyType.OrphanedPayment, "Erfolgreiche Zahlung ohne abgeschlossene Bestellung", $"Zahlung {payment.TransactionId} war erfolgreich, Bestellung {order.OrderNumber} steht aber weiterhin auf {order.Status}.", payment.Amount, order.Id, payment.Id, null, created, cancellationToken);

            if (payment is not null && payment.Status == PaymentStatus.Succeeded && Math.Abs(order.TotalAmount - payment.Amount) > 0.01m)
                await AddIfMissing(tenantId, DiscrepancyType.AmountMismatch, "Betragsabweichung", $"Bestellung {order.OrderNumber}: {order.TotalAmount:0.00} EUR, Zahlung: {payment.Amount:0.00} EUR.", Math.Abs(order.TotalAmount - payment.Amount), order.Id, payment.Id, null, created, cancellationToken);
        }

        foreach (var group in invoiceList.GroupBy(i => i.InvoiceNumber, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1))
            foreach (var invoice in group.Skip(1))
                await AddIfMissing(tenantId, DiscrepancyType.DuplicateInvoice, "Doppelte Rechnungsnummer", $"Die Rechnungsnummer {invoice.InvoiceNumber} kommt mehrfach vor.", invoice.Amount, null, null, invoice.Id, created, cancellationToken);

        foreach (var group in invoiceList.GroupBy(i => new { Supplier = i.SupplierName.ToLowerInvariant(), Day = i.InvoiceDateUtc.Date, i.Amount }).Where(g => g.Count() > 1))
            foreach (var invoice in group.Skip(1))
                await AddIfMissing(tenantId, DiscrepancyType.DuplicateInvoice, "Verdächtige doppelte Rechnung", $"Lieferant {invoice.SupplierName} hat am {invoice.InvoiceDateUtc:dd.MM.yyyy} mehrere Rechnungen über {invoice.Amount:0.00} EUR.", invoice.Amount, null, null, invoice.Id, created, cancellationToken);

        await discrepancies.SaveAsync(cancellationToken);
        return new ReconciliationResult(created.Count, created.Select(Map).ToList());
    }

    private async Task AddIfMissing(Guid tenantId, DiscrepancyType type, string title, string description, decimal amount, Guid? orderId, Guid? paymentId, Guid? invoiceId, List<Discrepancy> created, CancellationToken ct)
    {
        if (await discrepancies.ExistsOpenAsync(tenantId, type, orderId, paymentId, invoiceId, ct)) return;
        var item = Discrepancy.Create(tenantId, type, title, description, amount, orderId, paymentId, invoiceId);
        await discrepancies.AddAsync(item, ct);
        created.Add(item);
    }

    private static DiscrepancyDto Map(Discrepancy x)
        => new(x.Id, x.Type, x.Status, x.Title, x.Description, x.AffectedAmount, x.OrderId, x.PaymentId, x.InvoiceId, x.DetectedAtUtc, x.AiAnalysis);
}
