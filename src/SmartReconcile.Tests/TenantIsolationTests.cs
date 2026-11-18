using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SmartReconcile.API.Authentication;
using SmartReconcile.API.Controllers;
using SmartReconcile.Application.Abstractions;
using SmartReconcile.Application.DTOs;
using SmartReconcile.Domain.Entities;
using SmartReconcile.Domain.Enums;
using SmartReconcile.Infrastructure.Persistence;
using SmartReconcile.Infrastructure.Repositories;
using SmartReconcile.Infrastructure.Services;
using System.Text;
using Xunit;

namespace SmartReconcile.Tests;

public sealed class TenantIsolationTests
{
    [Fact]
    public async Task Bearer_token_for_shop_a_cannot_read_shop_b_payments_in_api()
    {
        await using var db = CreateDb();
        var tenantA = Tenant.Create("Shop A", "shop-a");
        var tenantB = Tenant.Create("Shop B", "shop-b");
        await db.Tenants.AddRangeAsync(tenantA, tenantB);

        var orderA = Order.Create("SHOPIFY-#A", "A", "", 100m, OrderStatus.Paid, DateTime.UtcNow, tenantId: tenantA.Id);
        var orderB = Order.Create("SHOPIFY-#B", "B", "", 200m, OrderStatus.Paid, DateTime.UtcNow, tenantId: tenantB.Id);
        await db.Orders.AddRangeAsync(orderA, orderB);
        await db.Payments.AddAsync(Payment.Create(tenantA.Id, orderA.Id, "A-PAY", 100m, PaymentStatus.Succeeded, DateTime.UtcNow));
        await db.Payments.AddAsync(Payment.Create(tenantB.Id, orderB.Id, "B-PAY", 200m, PaymentStatus.Succeeded, DateTime.UtcNow));
        await db.SaveChangesAsync();

        var controller = new PaymentsController(new SmartReconcile.Application.Services.PaymentService(new PaymentRepository(db), new OrderRepository(db)));
        var http = await AuthenticateAsync(tenantA);
        controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext { HttpContext = http };

        var result = await controller.Get(CancellationToken.None);
        var ok = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result.Result);
        var payments = Assert.IsAssignableFrom<IReadOnlyList<PaymentDto>>(ok.Value);

        Assert.Single(payments);
        Assert.Equal("A-PAY", payments[0].TransactionId);
    }

    [Fact]
    public async Task Shop_a_DATEV_export_does_not_contain_shop_b_bookings()
    {
        await using var db = CreateDb();
        var tenantA = Tenant.Create("Shop A", "shop-a-datev");
        var tenantB = Tenant.Create("Shop B", "shop-b-datev");
        await db.Tenants.AddRangeAsync(tenantA, tenantB);
        await db.AccountingSettings.AddRangeAsync(
            AccountingSettings.CreateDefault(tenantA.Id),
            AccountingSettings.CreateDefault(tenantB.Id));

        var orderA = Order.Create("SHOPIFY-#A-DATEV", "A", "", 100m, OrderStatus.Paid, new DateTime(2026,11,18,10,0,0,DateTimeKind.Utc), tenantId: tenantA.Id);
        var orderB = Order.Create("SHOPIFY-#B-DATEV", "B", "", 200m, OrderStatus.Paid, new DateTime(2026,11,18,11,0,0,DateTimeKind.Utc), tenantId: tenantB.Id);
        await db.Orders.AddRangeAsync(orderA, orderB);
        await db.Payments.AddRangeAsync(
            Payment.Create(tenantA.Id, orderA.Id, "A-DATEV-PAY", 100m, PaymentStatus.Succeeded, new DateTime(2026,11,18,10,5,0,DateTimeKind.Utc)),
            Payment.Create(tenantB.Id, orderB.Id, "B-DATEV-PAY", 200m, PaymentStatus.Succeeded, new DateTime(2026,11,18,11,5,0,DateTimeKind.Utc)));
        await db.SaveChangesAsync();

        var settings = await db.AccountingSettings.FirstAsync(x => x.TenantId == tenantA.Id);
        var exporter = new DatevExtfExporter(db, new TestAccountingSettingsService(settings));

        var bytes = await exporter.ExportAsync(tenantA.Id, new DateOnly(2026,11,18), new DateOnly(2026,11,18), false, CancellationToken.None);
        var csv = Decode(bytes);

        Assert.Contains("SHOPIFY-#A-DATEV", csv);
        Assert.DoesNotContain("SHOPIFY-#B-DATEV", csv);
        Assert.DoesNotContain("B-DATEV-PAY", csv);
    }

    [Fact]
    public async Task Bearer_token_for_shop_a_cannot_update_shop_b_discrepancy()
    {
        await using var db = CreateDb();
        var tenantA = Tenant.Create("Shop A", "shop-a-discrepancy");
        var tenantB = Tenant.Create("Shop B", "shop-b-discrepancy");
        await db.Tenants.AddRangeAsync(tenantA, tenantB);

        var discrepancyB = Discrepancy.Create(tenantB.Id, DiscrepancyType.AmountMismatch, "Shop B", "B only", 50m, null, null, null);
        await db.Discrepancies.AddAsync(discrepancyB);
        await db.SaveChangesAsync();

        var controller = new DiscrepanciesController(new SmartReconcile.Application.Services.DiscrepancyService(new DiscrepancyRepository(db)));
        var http = await AuthenticateAsync(tenantA);
        controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext { HttpContext = http };

        var result = await controller.UpdateStatus(discrepancyB.Id, new UpdateDiscrepancyStatusRequest(DiscrepancyStatus.Resolved), CancellationToken.None);

        Assert.IsType<Microsoft.AspNetCore.Mvc.NotFoundResult>(result.Result);
        Assert.Equal(DiscrepancyStatus.Open, await db.Discrepancies.Where(x => x.Id == discrepancyB.Id).Select(x => x.Status).SingleAsync());
    }

    private static async Task<HttpContext> AuthenticateAsync(Tenant tenant)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = "Bearer shop-a-token";
        var middleware = new ShopifyAuthenticationMiddleware(_ => Task.CompletedTask);
        await middleware.InvokeAsync(context, new FakeAuthenticationService(), new FakeTenantResolutionService(tenant));
        return context;
    }

    private static SmartReconcileDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<SmartReconcileDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new SmartReconcileDbContext(options);
    }

    private static string Decode(byte[] bytes)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        return Encoding.GetEncoding(1252).GetString(bytes);
    }

    private sealed class FakeAuthenticationService : IShopifyAuthenticationService
    {
        public Task<ShopifySession> AuthenticateAsync(string idToken, CancellationToken cancellationToken)
            => Task.FromResult(new ShopifySession("shop-a.myshopify.com", "test-user", "session", "token", DateTimeOffset.UtcNow.AddHours(1), false));
        public Task<ShopifyTokenExchangeResult> ExchangeIdTokenAsync(string shopDomain, string idToken, CancellationToken cancellationToken)
            => throw new NotSupportedException();
    }

    private sealed class FakeTenantResolutionService(Tenant tenant) : IShopifyTenantResolutionService
    {
        public Task<ShopifyTenantResolution> ResolveAsync(ShopifySession session, string idToken, CancellationToken cancellationToken)
            => Task.FromResult(new ShopifyTenantResolution(tenant, null!));
    }

    private sealed class TestAccountingSettingsService(AccountingSettings settings) : IAccountingSettingsService
    {
        public Task<AccountingSettingsDto> GetAsync(Guid tenantId, CancellationToken cancellationToken)
            => Task.FromResult(new AccountingSettingsDto(settings.Id, settings.TenantId, settings.Beraternummer, settings.Mandantennummer, settings.ChartOfAccounts, settings.RevenueAccount19, settings.RevenueAccount7, settings.PaymentClearingAccount, settings.FeeAccount, settings.UpdatedAtUtc));
        public Task<AccountingSettingsDto> UpdateAsync(Guid tenantId, UpdateAccountingSettingsRequest request, CancellationToken cancellationToken)
            => throw new NotSupportedException();
    }
}
