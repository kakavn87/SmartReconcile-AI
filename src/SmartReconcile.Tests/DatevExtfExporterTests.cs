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
            19m);

        var payment = Payment.Create(
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
            new DateOnly(2026, 11, 1),
            new DateOnly(2026, 11, 30),
            true,
            CancellationToken.None);

        var text = Decode(bytes);

        Assert.Contains("\"EXTF\";700;21;\"Buchungsstapel\";13;", text);
        Assert.Contains(";1001;10001;20260101;4;20261101;20261130;", text);
        Assert.Contains(";\"03\";", text);
        Assert.Contains("119,00;H;\"\";;;8400;1360;", text);
        Assert.Contains("2,90;S;\"\";;;4970;1360;", text);
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
            7m);

        await db.Orders.AddAsync(order);
        await db.SaveChangesAsync();

        var exporter = new DatevExtfExporter(db, new TestAccountingSettingsService(settings));

        var bytes = await exporter.ExportAsync(
            new DateOnly(2026, 11, 2),
            new DateOnly(2026, 11, 2),
            false,
            CancellationToken.None);

        var text = Decode(bytes);

        Assert.Contains("107,00;H;\"\";;;4300;1460;", text);
        Assert.Contains(";\"04\";", text);
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
            new DateTime(2026, 11, 3, 10, 0, 0, DateTimeKind.Utc));

        var payment = Payment.Create(
            order.Id,
            "PAY-TEST-5",
            119m,
            PaymentStatus.Succeeded,
            new DateTime(2026, 11, 3, 10, 5, 0, DateTimeKind.Utc));

        var discrepancy = Discrepancy.Create(
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
        public Task<AccountingSettingsDto> GetAsync(CancellationToken cancellationToken)
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
            UpdateAccountingSettingsRequest request,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();
    }
}
