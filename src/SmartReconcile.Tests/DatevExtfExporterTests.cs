using Xunit;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SmartReconcile.Application.Abstractions;
using SmartReconcile.Application.DTOs;
using SmartReconcile.Domain.Entities;
using SmartReconcile.Domain.Enums;
using SmartReconcile.Infrastructure.Persistence;
using SmartReconcile.Infrastructure.Services;

namespace SmartReconcile.Tests;

public sealed class DatevExtfExporterTests
{
    [Fact]
    public async Task Exporter_writes_extf_header_skr03_revenue_and_fee()
    {
        await using var db = CreateDb();
        var tenant = Tenant.Create("Test GmbH", "test");
        await db.Tenants.AddAsync(tenant);

        var settings = AccountingSettings.CreateDefault(tenant.Id);
        await db.AccountingSettings.AddAsync(settings);

        var order = Order.Create(
            "DE-TEST-1",
            "Max Mustermann",
            "max@example.de",
            119m,
            OrderStatus.Paid,
            new DateTime(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc),
            19m,
            tenantId: tenant.Id);

        var payment = Payment.Create(
            tenant.Id,
            order.Id,
            "PAY-TEST-1",
            119m,
            PaymentStatus.Succeeded,
            new DateTime(2026, 11, 1, 10, 5, 0, DateTimeKind.Utc),
            2.90m);

        await db.Orders.AddAsync(order);
        await db.Payments.AddAsync(payment);
        await db.SaveChangesAsync();

        var exporter = new DatevExtfExporter(db, new TestAccountingSettingsService(settings));

        var bytes = await exporter.ExportAsync(
            tenant.Id,
            new DateOnly(2026, 11, 1),
            new DateOnly(2026, 11, 30),
            true,
            CancellationToken.None);

        var text = Decode(bytes);

        Assert.Contains("\"EXTF\";700;21;\"Buchungsstapel\";13;", text);
        Assert.Contains(";1001;10001;20260101;4;20261101;20261130;\"SmartReconcile 11.2026\";", text);
        Assert.Contains(";\"03\";", text);
        Assert.Contains("119,00;S;", text);
        Assert.Contains(";1360;8400;", text);
        Assert.Contains("2,90;S;", text);
        Assert.Contains(";4970;1360;", text);
    }

    [Fact]
    public async Task Exporter_uses_skr04_defaults_and_7_percent_revenue_account()
    {
        await using var db = CreateDb();
        var tenant = Tenant.Create("Test GmbH", "test-skr04");
        await db.Tenants.AddAsync(tenant);

        var settings = AccountingSettings.CreateDefault(tenant.Id);
        settings.ApplyChartOfAccountsDefaults(ChartOfAccounts.SKR04);
        await db.AccountingSettings.AddAsync(settings);

        var order = Order.Create(
            "DE-TEST-4",
            "Max Mustermann",
            "max@example.de",
            107m,
            OrderStatus.Paid,
            new DateTime(2026, 11, 2, 10, 0, 0, DateTimeKind.Utc),
            7m,
            tenantId: tenant.Id);

        await db.Orders.AddAsync(order);
        await db.SaveChangesAsync();

        var exporter = new DatevExtfExporter(db, new TestAccountingSettingsService(settings));

        var bytes = await exporter.ExportAsync(
            tenant.Id,
            new DateOnly(2026, 11, 2),
            new DateOnly(2026, 11, 2),
            false,
            CancellationToken.None);

        var text = Decode(bytes);

        Assert.Contains("107,00;S;", text);
        Assert.Contains(";1460;4300;", text);
        Assert.Contains(";\"04\";", text);
    }

    [Fact]
    public async Task Refund_reverses_clearing_and_revenue_direction()
    {
        await using var db = CreateDb();
        var tenant = Tenant.Create("Test GmbH", "test-refund");
        await db.Tenants.AddAsync(tenant);

        var settings = AccountingSettings.CreateDefault(tenant.Id);
        await db.AccountingSettings.AddAsync(settings);

        var order = Order.Create(
            "DE-REFUND-1",
            "Max Mustermann",
            "max@example.de",
            119m,
            OrderStatus.Paid,
            new DateTime(2026, 11, 29, 10, 0, 0, DateTimeKind.Utc),
            19m,
            tenantId: tenant.Id);

        var payment = Payment.Create(
            tenant.Id,
            order.Id,
            "REFUND-1",
            50m,
            PaymentStatus.Succeeded,
            new DateTime(2026, 11, 29, 10, 5, 0, DateTimeKind.Utc),
            kind: PaymentKind.Refund);

        await db.Orders.AddAsync(order);
        await db.Payments.AddAsync(payment);
        await db.SaveChangesAsync();

        var exporter = new DatevExtfExporter(db, new TestAccountingSettingsService(settings));

        var bytes = await exporter.ExportAsync(
            tenant.Id,
            new DateOnly(2026, 11, 1),
            new DateOnly(2026, 11, 30),
            false,
            CancellationToken.None);

        var text = Decode(bytes);

        Assert.Contains("50,00;H;", text);
        Assert.Contains(";1360;8400;", text);
        Assert.Contains("Erstattung DE-REFUND-1", text);
        Assert.Contains("\r\n", text);
        Assert.DoesNotContain("\n", text.Replace("\r\n", string.Empty, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Exporter_includes_successful_refund_for_paid_order()
    {
        await using var db = CreateDb();
        var tenant = Tenant.Create("Test GmbH", "test-refund-export");
        await db.Tenants.AddAsync(tenant);
        var settings = AccountingSettings.CreateDefault(tenant.Id);
        await db.AccountingSettings.AddAsync(settings);

        var order = Order.Create(
            "SHOPIFY-#1007",
            "Shopify Kunde",
            "shop@example.de",
            3163.40m,
            OrderStatus.Paid,
            new DateTime(2026, 11, 29, 10, 0, 0, DateTimeKind.Utc),
            19m,
            tenantId: tenant.Id);

        var sale = Payment.Create(
            tenant.Id,
            order.Id,
            "SALE-1007",
            3163.40m,
            PaymentStatus.Succeeded,
            new DateTime(2026, 11, 29, 10, 5, 0, DateTimeKind.Utc));

        var refund = Payment.Create(
            tenant.Id,
            order.Id,
            "REFUND-1007",
            200m,
            PaymentStatus.Refunded,
            new DateTime(2026, 11, 29, 12, 0, 0, DateTimeKind.Utc),
            kind: PaymentKind.Refund);

        await db.Orders.AddAsync(order);
        await db.Payments.AddRangeAsync(sale, refund);
        await db.SaveChangesAsync();

        var exporter = new DatevExtfExporter(db, new TestAccountingSettingsService(settings));

        var bytes = await exporter.ExportAsync(
            tenant.Id,
            new DateOnly(2026, 11, 1),
            new DateOnly(2026, 11, 30),
            false,
            CancellationToken.None);

        var text = Decode(bytes);

        Assert.Contains("3163,40;S;", text);
        Assert.Contains(";1360;8400;", text);
        Assert.Contains("200,00;H;", text);
        Assert.Contains("Erstattung SHOPIFY-#1007", text);
    }

    [Fact]
    public async Task Exporter_includes_paid_order_even_when_it_has_open_discrepancy()
    {
        await using var db = CreateDb();
        var tenant = Tenant.Create("Test GmbH", "test-paid-discrepancy");
        await db.Tenants.AddAsync(tenant);
        var settings = AccountingSettings.CreateDefault(tenant.Id);
        await db.AccountingSettings.AddAsync(settings);

        var order = Order.Create(
            "PAID-DISC-1",
            "Shopify Kunde",
            "shop@example.de",
            500m,
            OrderStatus.Paid,
            new DateTime(2026, 11, 10, 10, 0, 0, DateTimeKind.Utc),
            19m,
            tenantId: tenant.Id);

        var payment = Payment.Create(
            tenant.Id,
            order.Id,
            "PAY-DISC-1",
            450m,
            PaymentStatus.Succeeded,
            new DateTime(2026, 11, 10, 10, 5, 0, DateTimeKind.Utc));

        var discrepancy = Discrepancy.Create(
            tenant.Id,
            DiscrepancyType.AmountMismatch,
            "Betragsabweichung",
            "Test",
            50m,
            order.Id,
            payment.Id,
            null);

        await db.Orders.AddAsync(order);
        await db.Payments.AddAsync(payment);
        await db.Discrepancies.AddAsync(discrepancy);
        await db.SaveChangesAsync();

        var exporter = new DatevExtfExporter(db, new TestAccountingSettingsService(settings));

        var bytes = await exporter.ExportAsync(
            tenant.Id,
            new DateOnly(2026, 11, 10),
            new DateOnly(2026, 11, 10),
            false,
            CancellationToken.None);

        var text = Decode(bytes);

        Assert.Contains("500,00;S;", text);
        Assert.Contains("PAID-DISC-1", text);
    }

    [Fact]
    public async Task Exporter_includes_usd_transaction_with_currency_and_rate()
    {
        await using var db = CreateDb();
        var tenant = Tenant.Create("Test GmbH", "test-usd");
        await db.Tenants.AddAsync(tenant);
        var settings = AccountingSettings.CreateDefault(tenant.Id);
        await db.AccountingSettings.AddAsync(settings);

        var order = Order.Create(
            "SHOPIFY-#1001",
            "Shopify Kunde",
            "shop@example.de",
            100m,
            OrderStatus.Paid,
            new DateTime(2026, 11, 15, 10, 0, 0, DateTimeKind.Utc),
            19m,
            currency: "USD",
            tenantId: tenant.Id);

        var payment = Payment.Create(
            tenant.Id,
            order.Id,
            "PAY-USD-1001",
            100m,
            PaymentStatus.Succeeded,
            new DateTime(2026, 11, 15, 10, 5, 0, DateTimeKind.Utc),
            currency: "USD");

        await db.Orders.AddAsync(order);
        await db.Payments.AddAsync(payment);
        await db.SaveChangesAsync();

        var exporter = new DatevExtfExporter(db, new TestAccountingSettingsService(settings));

        var bytes = await exporter.ExportAsync(
            tenant.Id,
            new DateOnly(2026, 11, 1),
            new DateOnly(2026, 11, 30),
            false,
            CancellationToken.None);

        var text = Decode(bytes);

        Assert.Contains("100,00;S;"USD";1,0000;", text);
        Assert.Contains(";1360;8400;", text);
        Assert.Contains("SHOPIFY-#1001", text);
    }

    [Fact]
    public async Task Only_reconciled_excludes_open_discrepancy()
    {
        await using var db = CreateDb();
        var tenant = Tenant.Create("Test GmbH", "test-reconciled");
        await db.Tenants.AddAsync(tenant);
        await db.AccountingSettings.AddAsync(AccountingSettings.CreateDefault(tenant.Id));

        var order = Order.Create(
            "DE-TEST-5",
            "Max Mustermann",
            "max@example.de",
            119m,
            OrderStatus.Paid,
            new DateTime(2026, 11, 3, 10, 0, 0, DateTimeKind.Utc),
            tenantId: tenant.Id);

        var payment = Payment.Create(
            tenant.Id,
            order.Id,
            "PAY-TEST-5",
            119m,
            PaymentStatus.Succeeded,
            new DateTime(2026, 11, 3, 10, 5, 0, DateTimeKind.Utc));

        var discrepancy = Discrepancy.Create(
            tenant.Id,
            DiscrepancyType.AmountMismatch,
            "Betragsabweichung",
            "Test",
            1m,
            order.Id,
            payment.Id,
            null);

        await db.Orders.AddAsync(order);
        await db.Payments.AddAsync(payment);
        await db.Discrepancies.AddAsync(discrepancy);
        await db.SaveChangesAsync();

        var settings = await db.AccountingSettings.FirstAsync();
        var exporter = new DatevExtfExporter(db, new TestAccountingSettingsService(settings));

        var bytes = await exporter.ExportAsync(
            tenant.Id,
            new DateOnly(2026, 11, 3),
            new DateOnly(2026, 11, 3),
            true,
            CancellationToken.None);

        var text = Decode(bytes);

        Assert.DoesNotContain("DE-TEST-5", text);
    }

    [Fact]
    public async Task Exporter_rejects_reversed_date_range()
    {
        await using var db = CreateDb();
        var tenant = Tenant.Create("Test GmbH", "test-invalid");
        await db.Tenants.AddAsync(tenant);
        var settings = AccountingSettings.CreateDefault(tenant.Id);
        await db.AccountingSettings.AddAsync(settings);
        await db.SaveChangesAsync();

        var exporter = new DatevExtfExporter(db, new TestAccountingSettingsService(settings));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            exporter.ExportAsync(
                tenant.Id,
                new DateOnly(2026, 11, 30),
                new DateOnly(2026, 11, 1),
                true,
                CancellationToken.None));
    }

    private static SmartReconcileDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<SmartReconcileDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new SmartReconcileDbContext(options);
    }

    private static string Decode(byte[] bytes)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        return Encoding.GetEncoding(1252).GetString(bytes);
    }

    private sealed class TestAccountingSettingsService(AccountingSettings settings) : IAccountingSettingsService
    {
        public Task<AccountingSettingsDto> GetAsync(Guid tenantId, CancellationToken cancellationToken)
            => Task.FromResult(new AccountingSettingsDto(
                settings.Id,
                settings.TenantId,
                settings.Beraternummer,
                settings.Mandantennummer,
                settings.ChartOfAccounts,
                settings.RevenueAccount19,
                settings.RevenueAccount7,
                settings.PaymentClearingAccount,
                settings.FeeAccount,
                settings.UpdatedAtUtc));

        public Task<AccountingSettingsDto> UpdateAsync(
            Guid tenantId,
            UpdateAccountingSettingsRequest request,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();
    }
}
