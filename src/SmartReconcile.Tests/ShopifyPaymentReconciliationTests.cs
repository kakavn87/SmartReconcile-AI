using Microsoft.EntityFrameworkCore;
using SmartReconcile.Application.Services;
using SmartReconcile.Domain.Entities;
using SmartReconcile.Domain.Enums;
using SmartReconcile.Infrastructure.Persistence;
using SmartReconcile.Infrastructure.Repositories;
using Xunit;

namespace SmartReconcile.Tests;

public sealed class ShopifyPaymentReconciliationTests
{
    [Fact]
    public async Task Reconciliation_creates_missing_mismatch_duplicate_and_currency_cases()
    {
        await using var db = new SmartReconcileDbContext(
            new DbContextOptionsBuilder<SmartReconcileDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);

        var tenant = Tenant.Create("Shop A", "shop-a-reconciliation");
        var connection = IntegrationConnection.CreateShopify(
            tenant.Id,
            "shop-a.myshopify.com",
            "Shop A",
            "encrypted-token",
            "read_orders");
        connection.SetShopCurrency("EUR");

        await db.Tenants.AddAsync(tenant);
        await db.IntegrationConnections.AddAsync(connection);

        var pending = Order.Create("SHOPIFY-#PENDING", "Customer", "", 100m, OrderStatus.Pending, DateTime.UtcNow, currency: "EUR", tenantId: tenant.Id);
        var mismatch = Order.Create("SHOPIFY-#MISMATCH", "Customer", "", 100m, OrderStatus.Paid, DateTime.UtcNow, currency: "EUR", tenantId: tenant.Id);
        var duplicate = Order.Create("SHOPIFY-#DUPLICATE", "Customer", "", 100m, OrderStatus.Paid, DateTime.UtcNow, currency: "EUR", tenantId: tenant.Id);
        var foreign = Order.Create("SHOPIFY-#USD", "Customer", "", 100m, OrderStatus.Paid, DateTime.UtcNow, currency: "USD", tenantId: tenant.Id);

        await db.Orders.AddRangeAsync(pending, mismatch, duplicate, foreign);
        await db.Payments.AddRangeAsync(
            Payment.Create(tenant.Id, mismatch.Id, "SHOPIFY-MISMATCH", 90m, PaymentStatus.Succeeded, DateTime.UtcNow, currency: "EUR", kind: PaymentKind.Sale, gateway: "shopify_payments"),
            Payment.Create(tenant.Id, duplicate.Id, "SHOPIFY-DUP-1", 50m, PaymentStatus.Succeeded, DateTime.UtcNow, currency: "EUR", kind: PaymentKind.Sale, gateway: "shopify_payments"),
            Payment.Create(tenant.Id, duplicate.Id, "SHOPIFY-DUP-2", 50m, PaymentStatus.Succeeded, DateTime.UtcNow, currency: "EUR", kind: PaymentKind.Sale, gateway: "shopify_payments"));
        await db.SaveChangesAsync();

        var service = new ReconciliationService(
            new OrderRepository(db),
            new PaymentRepository(db),
            new InvoiceRepository(db),
            new DiscrepancyRepository(db),
            new IntegrationConnectionRepository(db));

        var result = await service.ReconcileAsync(tenant.Id, CancellationToken.None);
        var discrepancies = await db.Discrepancies.Where(x => x.TenantId == tenant.Id).ToListAsync();

        Assert.Contains(discrepancies, x => x.Type == DiscrepancyType.MissingPayment && x.OrderId == pending.Id);
        Assert.Contains(discrepancies, x => x.Type == DiscrepancyType.AmountMismatch && x.OrderId == mismatch.Id);
        Assert.Contains(discrepancies, x => x.Type == DiscrepancyType.DuplicatePayment && x.OrderId == duplicate.Id);
        Assert.Contains(discrepancies, x => x.Type == DiscrepancyType.CurrencyMismatch && x.OrderId == foreign.Id);
        Assert.Equal(4, result.CreatedCount);
    }
}
