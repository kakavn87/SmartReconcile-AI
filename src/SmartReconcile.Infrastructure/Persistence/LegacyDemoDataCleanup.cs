using Microsoft.EntityFrameworkCore;

namespace SmartReconcile.Infrastructure.Persistence;

public static class LegacyDemoDataCleanup
{
    public static async Task RemoveAsync(SmartReconcileDbContext db, CancellationToken cancellationToken)
    {
        var demoOrderIds = await db.Orders
            .Where(x => x.OrderNumber.StartsWith("DE-2026-") &&
                        x.CustomerEmail.EndsWith("@example.de"))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        var demoPaymentIds = demoOrderIds.Count == 0
            ? []
            : await db.Payments
                .Where(x => demoOrderIds.Contains(x.OrderId))
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);

        var demoInvoiceIds = await db.Invoices
            .Where(x => x.InvoiceNumber.StartsWith("RE-2026-") ||
                        x.InvoiceNumber.StartsWith("RE-DUP-"))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        var demoEntityIds = demoOrderIds
            .Concat(demoPaymentIds)
            .Concat(demoInvoiceIds)
            .ToHashSet();

        if (demoEntityIds.Any())
        {
            var demoDiscrepancyIds = await db.Discrepancies
                .Where(x => (x.OrderId.HasValue && demoOrderIds.Contains(x.OrderId.Value)) ||
                            (x.PaymentId.HasValue && demoPaymentIds.Contains(x.PaymentId.Value)) ||
                            (x.InvoiceId.HasValue && demoInvoiceIds.Contains(x.InvoiceId.Value)))
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);

            if (demoDiscrepancyIds.Any())
            {
                var demoCases = await db.FinancialCases
                    .Where(x => demoDiscrepancyIds.Contains(x.DiscrepancyId))
                    .ToListAsync(cancellationToken);

                db.FinancialCases.RemoveRange(demoCases);
                db.Discrepancies.RemoveRange(
                    await db.Discrepancies
                        .Where(x => demoDiscrepancyIds.Contains(x.Id))
                        .ToListAsync(cancellationToken));
            }

            var graphRelations = await db.FinancialGraphRelations
                .Where(x => demoEntityIds.Contains(x.FromEntityId) || demoEntityIds.Contains(x.ToEntityId))
                .ToListAsync(cancellationToken);

            db.FinancialGraphRelations.RemoveRange(graphRelations);
        }

        if (demoOrderIds.Any())
        {
            db.Orders.RemoveRange(
                await db.Orders
                    .Where(x => demoOrderIds.Contains(x.Id))
                    .ToListAsync(cancellationToken));
        }

        if (demoInvoiceIds.Any())
        {
            db.Invoices.RemoveRange(
                await db.Invoices
                    .Where(x => demoInvoiceIds.Contains(x.Id))
                    .ToListAsync(cancellationToken));
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
