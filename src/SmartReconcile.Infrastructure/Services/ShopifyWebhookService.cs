using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SmartReconcile.Application.Abstractions;
using SmartReconcile.Domain.Entities;
using SmartReconcile.Domain.Enums;
using SmartReconcile.Infrastructure.Persistence;

namespace SmartReconcile.Infrastructure.Services;

public sealed class ShopifyWebhookService(
    IOptions<ShopifyOptions> options,
    SmartReconcileDbContext db,
    IReconciliationService reconciliation) : IShopifyWebhookService
{
    private readonly ShopifyOptions options = options.Value;

    public async Task<ShopifyWebhookResult> ProcessAsync(
        string rawBody,
        string hmacHeader,
        string? shopDomain,
        string? topic,
        string? webhookId,
        CancellationToken cancellationToken)
    {
        if (!VerifyHmac(rawBody, hmacHeader))
            throw new UnauthorizedAccessException("Ungültige Shopify-Webhook-Signatur.");

        if (string.IsNullOrWhiteSpace(webhookId))
            throw new ArgumentException("Shopify Webhook-ID fehlt.", nameof(webhookId));

        if (!string.IsNullOrWhiteSpace(shopDomain) && !IsShopDomain(shopDomain))
            throw new UnauthorizedAccessException("Ungültige Shopify-Shop-Domain.");

        var normalizedShopDomain = shopDomain?.Trim().ToLowerInvariant();
        var connection = await db.IntegrationConnections
            .FirstOrDefaultAsync(
                x => x.Provider == IntegrationProvider.Shopify &&
                     x.ShopDomain == normalizedShopDomain,
                cancellationToken);

        if (connection is null)
            throw new KeyNotFoundException("Shopify-Verbindung wurde nicht gefunden.");

        var duplicate = await db.WebhookEvents.AnyAsync(
            x => x.ConnectionId == connection.Id && x.ExternalEventId == webhookId,
            cancellationToken);

        if (duplicate)
            return new ShopifyWebhookResult(Guid.Empty, true, true, topic ?? "unknown", normalizedShopDomain);

        var eventType = topic ?? "unknown";
        var webhook = WebhookEvent.Create(
            connection.TenantId,
            connection.Id,
            webhookId,
            eventType,
            rawBody);

        connection.MarkWebhookReceived();
        db.WebhookEvents.Add(webhook);

        switch (eventType)
        {
            case "orders/create":
            case "orders/updated":
            case "orders/paid":
                await ImportOrderAsync(rawBody, cancellationToken);
                break;

            case "orders/cancelled":
                await CancelOrderAsync(rawBody, cancellationToken);
                break;

            case "refunds/create":
                await ProcessRefundAsync(connection.Id, rawBody, cancellationToken);
                break;
        }

        await db.SaveChangesAsync(cancellationToken);
        webhook.MarkProcessed();
        await db.SaveChangesAsync(cancellationToken);
        await reconciliation.ReconcileAsync(cancellationToken);

        return new ShopifyWebhookResult(
            webhook.Id,
            false,
            true,
            eventType,
            normalizedShopDomain);
    }

    private async Task ImportOrderAsync(string rawBody, CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(rawBody);
        var root = document.RootElement;
        if (!root.TryGetProperty("id", out var idProperty))
            return;

        var externalId = idProperty.ToString();
        var orderName = root.TryGetProperty("name", out var name)
            ? name.GetString() ?? externalId
            : externalId;
        var orderNumber = $"SHOPIFY-{orderName}";
        var order = await db.Orders.FirstOrDefaultAsync(x => x.OrderNumber == orderNumber, cancellationToken);
        var amount = ParseDecimal(root, "current_total_price", "total_price");
        var email = root.TryGetProperty("email", out var emailProperty)
            ? emailProperty.GetString() ?? string.Empty
            : string.Empty;
        var customerName = "Shopify Kunde";

        if (root.TryGetProperty("customer", out var customer) && customer.ValueKind == JsonValueKind.Object)
        {
            var first = customer.TryGetProperty("first_name", out var firstName) ? firstName.GetString() : null;
            var last = customer.TryGetProperty("last_name", out var lastName) ? lastName.GetString() : null;
            customerName = $"{first} {last}".Trim();
            if (string.IsNullOrWhiteSpace(customerName))
                customerName = "Shopify Kunde";
        }

        var financialStatus = root.TryGetProperty("financial_status", out var financial)
            ? financial.GetString()?.ToUpperInvariant() ?? "PENDING"
            : "PENDING";

        if (order is null)
        {
            var status = financialStatus == "PAID" ? OrderStatus.Paid : OrderStatus.Pending;
            var createdAt = root.TryGetProperty("created_at", out var created) &&
                            DateTime.TryParse(created.GetString(), out var parsedCreated)
                ? parsedCreated.ToUniversalTime()
                : DateTime.UtcNow;

            order = Order.Create(orderNumber, customerName, email, amount, status, createdAt);
            await db.Orders.AddAsync(order, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
        }
        else if (financialStatus == "PAID")
        {
            order.ChangeStatus(OrderStatus.Paid);
        }

        if (!root.TryGetProperty("transactions", out var transactions) ||
            transactions.ValueKind != JsonValueKind.Array)
            return;

        foreach (var transaction in transactions.EnumerateArray())
        {
            var kind = transaction.TryGetProperty("kind", out var kindProperty)
                ? kindProperty.GetString()
                : null;
            var status = transaction.TryGetProperty("status", out var statusProperty)
                ? statusProperty.GetString()
                : null;

            if (!string.Equals(status, "success", StringComparison.OrdinalIgnoreCase) ||
                (!string.Equals(kind, "sale", StringComparison.OrdinalIgnoreCase) &&
                 !string.Equals(kind, "capture", StringComparison.OrdinalIgnoreCase)))
                continue;

            if (!transaction.TryGetProperty("id", out var transactionId))
                continue;

            var externalTransactionId = $"SHOPIFY-{transactionId}";
            if (await db.Payments.AnyAsync(x => x.TransactionId == externalTransactionId, cancellationToken))
                continue;

            var transactionAmount = ParseDecimal(transaction, "amount");
            var paidAt = transaction.TryGetProperty("created_at", out var txCreated) &&
                         DateTime.TryParse(txCreated.GetString(), out var parsedPaid)
                ? parsedPaid.ToUniversalTime()
                : DateTime.UtcNow;

            await db.Payments.AddAsync(
                Payment.Create(
                    order.Id,
                    externalTransactionId,
                    transactionAmount,
                    PaymentStatus.Succeeded,
                    paidAt),
                cancellationToken);
        }
    }

    private async Task CancelOrderAsync(string rawBody, CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(rawBody);
        var root = document.RootElement;

        if (!root.TryGetProperty("id", out var idProperty))
            return;

        var externalId = idProperty.ToString();
        var orderName = root.TryGetProperty("name", out var name)
            ? name.GetString() ?? externalId
            : externalId;
        var orderNumber = $"SHOPIFY-{orderName}";

        var order = await db.Orders.FirstOrDefaultAsync(x => x.OrderNumber == orderNumber, cancellationToken);
        if (order is null)
        {
            await ImportOrderAsync(rawBody, cancellationToken);
            order = await db.Orders.FirstOrDefaultAsync(x => x.OrderNumber == orderNumber, cancellationToken);
        }

        order?.ChangeStatus(OrderStatus.Cancelled);
    }

    private async Task ProcessRefundAsync(
        Guid connectionId,
        string rawBody,
        CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(rawBody);
        var root = document.RootElement;

        var orderId = root.TryGetProperty("order_id", out var orderIdProperty)
            ? orderIdProperty.ToString()
            : null;

        var successfulRefundAmount = 0m;
        var parentTransactionIds = new HashSet<string>(StringComparer.Ordinal);

        if (root.TryGetProperty("transactions", out var transactions) &&
            transactions.ValueKind == JsonValueKind.Array)
        {
            foreach (var transaction in transactions.EnumerateArray())
            {
                var kind = transaction.TryGetProperty("kind", out var kindProperty)
                    ? kindProperty.GetString()
                    : null;
                var status = transaction.TryGetProperty("status", out var statusProperty)
                    ? statusProperty.GetString()
                    : null;

                if (!string.Equals(kind, "refund", StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(status, "success", StringComparison.OrdinalIgnoreCase))
                    continue;

                successfulRefundAmount += ParseDecimal(transaction, "amount");

                if (transaction.TryGetProperty("parent_id", out var parentId) &&
                    parentId.ValueKind != JsonValueKind.Null &&
                    !string.IsNullOrWhiteSpace(parentId.ToString()))
                {
                    parentTransactionIds.Add(parentId.ToString());
                }
            }
        }

        if (successfulRefundAmount <= 0m)
            return;

        var payments = new List<Payment>();

        foreach (var parentId in parentTransactionIds)
        {
            var payment = await db.Payments.FirstOrDefaultAsync(
                x => x.TransactionId == $"SHOPIFY-{parentId}",
                cancellationToken);

            if (payment is not null)
                payments.Add(payment);
        }

        if (payments.Count == 0 && !string.IsNullOrWhiteSpace(orderId))
        {
            var order = await FindOrderByShopifyIdAsync(connectionId, orderId, cancellationToken);
            if (order is not null)
            {
                payments = await db.Payments
                    .Where(x => x.OrderId == order.Id && x.Status == PaymentStatus.Succeeded)
                    .ToListAsync(cancellationToken);
            }
        }

        if (payments.Count == 0)
            return;

        var distinctPayments = payments.DistinctBy(x => x.Id).ToList();
        var refundTarget = distinctPayments.Count == 1 ? distinctPayments[0] : null;

        if (refundTarget is not null && successfulRefundAmount >= refundTarget.Amount)
        {
            refundTarget.ChangeStatus(PaymentStatus.Refunded);
            var order = await db.Orders.FirstOrDefaultAsync(x => x.Id == refundTarget.OrderId, cancellationToken);
            order?.ChangeStatus(OrderStatus.Refunded);
            return;
        }

        if (!string.IsNullOrWhiteSpace(orderId))
        {
            var order = await FindOrderByShopifyIdAsync(connectionId, orderId, cancellationToken);
            if (order is not null)
            {
                var totalPaid = distinctPayments.Sum(x => x.Amount);
                if (successfulRefundAmount >= totalPaid)
                {
                    foreach (var payment in distinctPayments)
                        payment.ChangeStatus(PaymentStatus.Refunded);

                    order.ChangeStatus(OrderStatus.Refunded);
                }
            }
        }
    }

    private async Task<Order?> FindOrderByShopifyIdAsync(
        Guid connectionId,
        string shopifyOrderId,
        CancellationToken cancellationToken)
    {
        var events = await db.WebhookEvents
            .Where(x => x.ConnectionId == connectionId &&
                        (x.EventType == "orders/create" ||
                         x.EventType == "orders/updated" ||
                         x.EventType == "orders/paid" ||
                         x.EventType == "orders/cancelled"))
            .OrderByDescending(x => x.ReceivedAtUtc)
            .ToListAsync(cancellationToken);

        foreach (var webhook in events)
        {
            try
            {
                using var document = JsonDocument.Parse(webhook.PayloadJson);
                if (!document.RootElement.TryGetProperty("id", out var idProperty) ||
                    idProperty.ToString() != shopifyOrderId)
                    continue;

                var orderName = document.RootElement.TryGetProperty("name", out var name)
                    ? name.GetString() ?? shopifyOrderId
                    : shopifyOrderId;

                return await db.Orders.FirstOrDefaultAsync(
                    x => x.OrderNumber == $"SHOPIFY-{orderName}",
                    cancellationToken);
            }
            catch (JsonException)
            {
                // Ignore malformed historical webhook payloads.
            }
        }

        return null;
    }

    private static decimal ParseDecimal(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (root.TryGetProperty(name, out var value) &&
                decimal.TryParse(
                    value.ToString(),
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var parsed))
                return parsed;
        }

        return 0m;
    }

    private bool VerifyHmac(string rawBody, string supplied)
    {
        if (string.IsNullOrWhiteSpace(supplied) || string.IsNullOrWhiteSpace(options.ClientSecret))
            return false;

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(options.ClientSecret));
        var digest = Convert.ToBase64String(
            hmac.ComputeHash(Encoding.UTF8.GetBytes(rawBody)));

        try
        {
            return CryptographicOperations.FixedTimeEquals(
                Convert.FromBase64String(digest),
                Convert.FromBase64String(supplied));
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static bool IsShopDomain(string value)
    {
        var shop = value.Trim().ToLowerInvariant();
        return shop.EndsWith(".myshopify.com", StringComparison.Ordinal)
            && shop.Count(c => c == '.') == 2
            && shop.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '.');
    }
}