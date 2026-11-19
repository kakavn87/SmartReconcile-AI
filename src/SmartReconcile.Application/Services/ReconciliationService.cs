using SmartReconcile.Application.Abstractions;
using SmartReconcile.Application.DTOs;
using SmartReconcile.Domain.Entities;
using SmartReconcile.Domain.Enums;

namespace SmartReconcile.Application.Services;

public sealed class ReconciliationService(
    IOrderRepository orders,
    IPaymentRepository payments,
    IInvoiceRepository invoices,
    IDiscrepancyRepository discrepancies,
    IIntegrationConnectionRepository connections) : IReconciliationService
{
    public async Task<ReconciliationResult> ReconcileAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var orderList = await orders.GetAllAsync(tenantId, cancellationToken);
        var paymentList = await payments.GetAllAsync(tenantId, cancellationToken);
        var invoiceList = await invoices.GetAllAsync(tenantId, cancellationToken);
        var connection = (await connections.GetForTenantAsync(tenantId, cancellationToken))
            .FirstOrDefault(x => x.Provider == IntegrationProvider.Shopify && x.Status is not ConnectionStatus.Revoked and not ConnectionStatus.Disconnected);
        var shopCurrency = connection?.ShopCurrency ?? "EUR";

        var created = new List<Discrepancy>();
        var now = DateTime.UtcNow;

        foreach (var order in orderList)
        {
            var orderPayments = paymentList
                .Where(p => p.OrderId == order.Id && p.TenantId == tenantId)
                .ToList();

            var successfulPayments = orderPayments
                .Where(p => p.Status == PaymentStatus.Succeeded &&
                            p.Kind is PaymentKind.Sale or PaymentKind.Capture)
                .ToList();

            var refunds = orderPayments
                .Where(p => p.Status == PaymentStatus.Refunded && p.Kind == PaymentKind.Refund)
                .ToList();

            var netCollected = decimal.Round(successfulPayments.Sum(p => p.Amount) - refunds.Sum(p => p.Amount), 2);
            var hasSuccessfulPayment = successfulPayments.Count > 0;
            var comparablePayments = orderPayments.Where(p => string.Equals(p.Currency, order.Currency, StringComparison.OrdinalIgnoreCase)).ToList();

            if (order.Status == OrderStatus.Pending && !hasSuccessfulPayment)
            {
                await AddIfMissing(
                    tenantId,
                    DiscrepancyType.MissingPayment,
                    "Zahlung fehlt",
                    $"Bestellung {order.OrderNumber} ist offen und hat keine erfolgreiche Zahlungstransaktion.",
                    order.TotalAmount,
                    order.Id,
                    null,
                    null,
                    created,
                    cancellationToken);
            }

            if (hasSuccessfulPayment && order.Currency.Equals(shopCurrency, StringComparison.OrdinalIgnoreCase))
            {
                var comparableNet = decimal.Round(
                    comparablePayments.Where(p => p.Status == PaymentStatus.Succeeded && (p.Kind is PaymentKind.Sale or PaymentKind.Capture)).Sum(p => p.Amount) -
                    comparablePayments.Where(p => p.Status == PaymentStatus.Refunded && p.Kind == PaymentKind.Refund).Sum(p => p.Amount),
                    2);

                if (Math.Abs(order.TotalAmount - comparableNet) > 0.01m)
                {
                    var anchorPayment = successfulPayments.FirstOrDefault();
                    await AddIfMissing(
                        tenantId,
                        DiscrepancyType.AmountMismatch,
                        "Betragsabweichung",
                        $"Bestellung {order.OrderNumber}: {order.TotalAmount:0.00} {order.Currency}, netto Zahlungseingang: {comparableNet:0.00} {order.Currency}.",
                        Math.Abs(order.TotalAmount - comparableNet),
                        order.Id,
                        anchorPayment?.Id,
                        null,
                        created,
                        cancellationToken);
                }
            }

            if (!order.Currency.Equals(shopCurrency, StringComparison.OrdinalIgnoreCase))
            {
                await AddIfMissing(
                    tenantId,
                    DiscrepancyType.CurrencyMismatch,
                    "Währungsabweichung",
                    $"Bestellung {order.OrderNumber} verwendet {order.Currency}; Shop-Währung ist {shopCurrency}.",
                    order.TotalAmount,
                    order.Id,
                    successfulPayments.FirstOrDefault()?.Id,
                    null,
                    created,
                    cancellationToken);
            }

            foreach (var duplicateGroup in successfulPayments
                         .GroupBy(p => new { p.Amount, Currency = p.Currency.ToUpperInvariant(), Gateway = p.Gateway?.ToLowerInvariant() ?? string.Empty, p.Kind })
                         .Where(g => g.Count() > 1))
            {
                foreach (var duplicate in duplicateGroup.Skip(1))
                {
                    await AddIfMissing(
                        tenantId,
                        DiscrepancyType.DuplicatePayment,
                        "Doppelte Zahlung",
                        $"Für Bestellung {order.OrderNumber} wurden mehrere identische {duplicate.Kind} Zahlungstransaktionen über {duplicate.Amount:0.00} {duplicate.Currency} beim Gateway {duplicate.Gateway ?? "unbekannt"} gefunden.",
                        duplicate.Amount,
                        order.Id,
                        duplicate.Id,
                        null,
                        created,
                        cancellationToken);
                }
            }

            if (hasSuccessfulPayment && (order.Status == OrderStatus.Pending || order.Status == OrderStatus.OnHold))
            {
                var payment = successfulPayments.First();
                await AddIfMissing(
                    tenantId,
                    DiscrepancyType.OrphanedPayment,
                    "Erfolgreiche Zahlung ohne abgeschlossene Bestellung",
                    $"Zahlung {payment.TransactionId} war erfolgreich, Bestellung {order.OrderNumber} steht aber weiterhin auf {order.Status}.",
                    payment.Amount,
                    order.Id,
                    payment.Id,
                    null,
                    created,
                    cancellationToken);
            }
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
