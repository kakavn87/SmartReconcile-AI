using Microsoft.EntityFrameworkCore;
using SmartReconcile.Application.Abstractions;
using SmartReconcile.Domain.Entities;
using SmartReconcile.Domain.Enums;
using SmartReconcile.Infrastructure.Persistence;

namespace SmartReconcile.Infrastructure.Services;

public sealed class ShopifySyncService(
    SmartReconcileDbContext db,
    IShopifyClient shopifyClient,
    IReconciliationService reconciliation) : IShopifySyncService
{
    public async Task<ShopifySyncResult> SyncAsync(Guid connectionId, CancellationToken cancellationToken)
    {
        var connection = await db.IntegrationConnections.FirstOrDefaultAsync(x => x.Id == connectionId, cancellationToken)
            ?? throw new KeyNotFoundException("Shopify-Verbindung wurde nicht gefunden.");

        if (connection.Provider != IntegrationProvider.Shopify)
            throw new InvalidOperationException("Die Verbindung ist keine Shopify-Verbindung.");

        var orders = await shopifyClient.GetLatestOrdersAsync(connection, cancellationToken);
        var importedOrders = 0;
        var importedPayments = 0;
        var skipped = 0;

        foreach (var source in orders)
        {
            var orderNumber = $"SHOPIFY-{source.OrderNumber}";
            var status = source.FinancialStatus.ToUpperInvariant() switch
            {
                "PAID" => OrderStatus.Paid,
                "PARTIALLY_PAID" => OrderStatus.Paid,
                "AUTHORIZED" => OrderStatus.Pending,
                "REFUNDED" => OrderStatus.Refunded,
                "CANCELLED" => OrderStatus.Cancelled,
                _ => OrderStatus.Pending
            };

            var order = await db.Orders.FirstOrDefaultAsync(
                x => x.OrderNumber == orderNumber,
                cancellationToken);

            if (order is not null)
            {
                order.UpdateFromShopify(
                    source.TotalAmount,
                    source.Currency,
                    status,
                    source.CreatedAtUtc);
                skipped++;
            }
            else
            {
                order = Order.Create(
                    orderNumber,
                    source.CustomerName,
                    source.CustomerEmail ?? string.Empty,
                    source.TotalAmount,
                    status,
                    source.CreatedAtUtc,
                    currency: source.Currency);

                await db.Orders.AddAsync(order, cancellationToken);
                importedOrders++;
            }

            foreach (var transaction in source.Transactions.Where(x =>
                         string.Equals(x.Status, "SUCCESS", StringComparison.OrdinalIgnoreCase) &&
                         (string.Equals(x.Kind, "SALE", StringComparison.OrdinalIgnoreCase) ||
                          string.Equals(x.Kind, "CAPTURE", StringComparison.OrdinalIgnoreCase))))
            {
                var payment = Payment.Create(
                    order.Id,
                    $"SHOPIFY-{transaction.ExternalTransactionId}",
                    transaction.Amount,
                    PaymentStatus.Succeeded,
                    DateTime.UtcNow);

                await db.Payments.AddAsync(payment, cancellationToken);
                importedPayments++;
            }
        }

        connection.MarkSynced();
        await db.SaveChangesAsync(cancellationToken);
        await reconciliation.ReconcileAsync(cancellationToken);

        return new ShopifySyncResult(
            connectionId,
            importedOrders,
            importedPayments,
            skipped,
            DateTime.UtcNow);
    }
}
