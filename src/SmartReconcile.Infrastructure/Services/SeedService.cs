using Microsoft.EntityFrameworkCore;
using SmartReconcile.Application.Abstractions;
using SmartReconcile.Application.DTOs;
using SmartReconcile.Domain.Entities;
using SmartReconcile.Domain.Enums;
using SmartReconcile.Infrastructure.Persistence;

namespace SmartReconcile.Infrastructure.Services;

public sealed class SeedService(SmartReconcileDbContext db) : ISeedService
{
    public async Task<SeedResult> GenerateAsync(CancellationToken ct)
    {
        await db.Database.EnsureCreatedAsync(ct);
        if (await db.Orders.AnyAsync(ct)) return new SeedResult(await db.Orders.CountAsync(ct), await db.Payments.CountAsync(ct), await db.Invoices.CountAsync(ct), 0);

        var now = DateTime.UtcNow;
        var orders = new List<Order>();
        var payments = new List<Payment>();
        var invoices = new List<Invoice>();
        for (var i = 1; i <= 20; i++)
        {
            var status = i <= 16 ? OrderStatus.Paid : i == 17 ? OrderStatus.Pending : i == 18 ? OrderStatus.Pending : OrderStatus.Paid;
            var age = i == 17 ? 72 : 12;
            var amount = 50m + i * 17.50m;
            var order = Order.Create($"DE-{2026}-{i:0000}", $"Kunde {i}", $"kunde{i}@example.de", amount, status, now.AddHours(-age));
            orders.Add(order);
            if (i != 17)
            {
                var paymentAmount = i == 19 ? amount - 10m : i == 18 ? amount : amount;
                var paymentStatus = i == 18 ? PaymentStatus.Succeeded : PaymentStatus.Succeeded;
                payments.Add(Payment.Create(order.Id, $"PAY-{i:00000}", paymentAmount, paymentStatus, now.AddHours(-age + 1)));
            }
            invoices.Add(Invoice.Create($"RE-{2026}-{i:0000}", i % 2 == 0 ? "Muster GmbH" : "Demo Handels GmbH", amount, now.AddDays(-i), $"Rechnung RE-2026-{i:0000}
Lieferant: Demo Handels GmbH
Betrag: {amount:0.00} EUR
MwSt: 19%"));
        }
        invoices.Add(Invoice.Create("RE-2026-0001", "Demo Handels GmbH", 67.50m, now.AddDays(-1), "Duplikat der Rechnungsnummer RE-2026-0001"));
        invoices.Add(Invoice.Create("RE-DUP-01", "Muster GmbH", 100m, now.Date, "Erste Rechnung gleicher Lieferant und gleicher Betrag"));
        invoices.Add(Invoice.Create("RE-DUP-02", "Muster GmbH", 100m, now.Date.AddHours(2), "Zweite Rechnung gleicher Lieferant und gleicher Betrag"));

        await db.Orders.AddRangeAsync(orders, ct);
        await db.Payments.AddRangeAsync(payments, ct);
        await db.Invoices.AddRangeAsync(invoices, ct);
        await db.SaveChangesAsync(ct);
        return new SeedResult(orders.Count, payments.Count, invoices.Count, 5);
    }
}