using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartReconcile.Application.Abstractions;
using SmartReconcile.Domain.Entities;
using SmartReconcile.Domain.Enums;
using SmartReconcile.Infrastructure.Persistence;

namespace SmartReconcile.Infrastructure.Services;

public sealed class ShopifySyncService(
    SmartReconcileDbContext db,
    IShopifyClient shopifyClient,
    IReconciliationService reconciliation,
    ILogger<ShopifySyncService> logger) : IShopifySyncService
{
    public async Task<ShopifySyncResult> SyncAsync(Guid connectionId, CancellationToken cancellationToken)
    {
        var connection = await db.IntegrationConnections.FirstOrDefaultAsync(x => x.Id == connectionId, cancellationToken)
            ?? throw new KeyNotFoundException("Shopify-Verbindung wurde nicht gefunden.");

        if (connection.Provider != IntegrationProvider.Shopify)
            throw new InvalidOperationException("Die Verbindung ist keine Shopify-Verbindung.");

        var orders = await shopifyClient.GetLatestOrdersAsync(connection, cancellationToken);
        logger.LogInformation(
            "Shopify order sync started for connection {ConnectionId}: received {OrderCount} orders",
            connectionId,
            orders.Count);

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
                x => x.TenantId == connection.TenantId && x.OrderNumber == orderNumber,
                cancellationToken);

            if (order is not null)
            {
                logger.LogInformation(
                    "Updating Shopify order {OrderNumber}: amount={Amount} {Currency}, status={Status}, createdAt={CreatedAtUtc}",
                    orderNumber,
                    source.TotalAmount,
                    source.Currency,
                    status,
                    source.CreatedAtUtc);

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
                    currency: source.Currency,
                    tenantId: connection.TenantId);

                await db.Orders.AddAsync(order, cancellationToken);
                importedOrders++;
            }

            foreach (var transaction in source.Transactions.Where(x =>
                         string.Equals(x.Status, "SUCCESS", StringComparison.OrdinalIgnoreCase) &&
                         (string.Equals(x.Kind, "SALE", StringComparison.OrdinalIgnoreCase) ||
                          string.Equals(x.Kind, "CAPTURE", StringComparison.OrdinalIgnoreCase))))
            {
                var transactionId = $"SHOPIFY-{transaction.ExternalTransactionId}";
                if (await db.Payments.AnyAsync(
                        x => x.TenantId == connection.TenantId && x.TransactionId == transactionId,
                        cancellationToken))
                    continue;

                var payment = Payment.Create(
                    connection.TenantId,
                    order.Id,
                    transactionId,
                    transaction.Amount,
                    PaymentStatus.Succeeded,
                    transaction.CreatedAtUtc == default ? DateTime.UtcNow : transaction.CreatedAtUtc);

                await db.Payments.AddAsync(payment, cancellationToken);
                importedPayments++;
            }
        }

        connection.MarkSynced();
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Shopify order sync saved for connection {ConnectionId}: importedOrders={ImportedOrders}, updatedOrders={UpdatedOrders}, importedPayments={ImportedPayments}",
            connectionId,
            importedOrders,
            skipped,
            importedPayments);
        await reconciliation.ReconcileAsync(connection.TenantId, cancellationToken);

        return new ShopifySyncResult(
            connectionId,
            importedOrders,
            importedPayments,
            skipped,
            DateTime.UtcNow);
    }
}
