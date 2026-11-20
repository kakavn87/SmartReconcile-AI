using Microsoft.EntityFrameworkCore;
using SmartReconcile.Domain.Entities;
using SmartReconcile.Domain.Enums;
using SmartReconcile.Infrastructure.Persistence;

namespace SmartReconcile.Infrastructure.Services;

public sealed class InvoiceAutomationService(
    SmartReconcileDbContext db,
    InvoiceNumberService numbers,
    InvoicePdfService pdf)
{
    public async Task<Invoice?> EnsureSalesInvoiceAsync(
        Guid tenantId,
        string shopifyOrderId,
        Order order,
        CancellationToken cancellationToken)
    {
        if (tenantId == Guid.Empty || order.TenantId != tenantId || order.Status != OrderStatus.Paid)
            return null;

        var settings = await db.AccountingSettings.FirstOrDefaultAsync(x => x.TenantId == tenantId, cancellationToken);
        if (settings is not null && !settings.AutomaticInvoiceCreationEnabled)
            return null;

        var existing = await db.Invoices.FirstOrDefaultAsync(
            x => x.TenantId == tenantId && x.OrderId == order.Id && x.DocumentType == InvoiceDocumentType.Invoice,
            cancellationToken);
        if (existing is not null)
            return existing;

        var invoiceNumber = await numbers.NextAsync(tenantId, DateTime.UtcNow.Year, "RE", cancellationToken);
        var taxType = order.TaxRate == 7m ? InvoiceTaxType.Reduced : order.TaxRate == 0m ? InvoiceTaxType.Exempt : InvoiceTaxType.Standard;
        var invoice = Invoice.Create(
            tenantId,
            InvoiceType.Sales,
            invoiceNumber,
            order.CustomerName,
            order.TotalAmount,
            order.Currency,
            order.CreatedAtUtc,
            $"Automatisch aus Shopify-Bestellung {shopifyOrderId} erzeugt.",
            order.Id,
            order.TaxRate,
            taxType,
            InvoiceDocumentType.Invoice,
            customerEmail: order.CustomerEmail,
            customerAddress: null,
            shopifyOrderId: shopifyOrderId);

        db.Invoices.Add(invoice);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            var concurrent = await db.Invoices.FirstOrDefaultAsync(
                x => x.TenantId == tenantId && x.OrderId == order.Id && x.DocumentType == InvoiceDocumentType.Invoice,
                cancellationToken);
            if (concurrent is not null)
                return concurrent;
            throw;
        }

        if (settings is not null)
            invoice.AttachPdf(pdf.Generate(invoice, settings));
        else
            invoice.AttachPdf(pdf.Generate(invoice, AccountingSettings.CreateDefault(tenantId)));

        await db.SaveChangesAsync(cancellationToken);
        return invoice;
    }

    public async Task<Invoice?> EnsureStornoInvoiceAsync(
        Guid tenantId,
        Order order,
        string refundId,
        decimal amount,
        string currency,
        DateTime refundDateUtc,
        CancellationToken cancellationToken)
    {
        if (tenantId == Guid.Empty || order.TenantId != tenantId || string.IsNullOrWhiteSpace(refundId))
            return null;

        var existing = await db.Invoices.FirstOrDefaultAsync(
            x => x.TenantId == tenantId && x.RefundId == refundId,
            cancellationToken);
        if (existing is not null)
            return existing;

        var original = await db.Invoices.FirstOrDefaultAsync(
            x => x.TenantId == tenantId &&
                 x.OrderId == order.Id &&
                 x.DocumentType == InvoiceDocumentType.Invoice,
            cancellationToken);

        // A refund must never manufacture a new sales invoice after the order was
        // cancelled/refunded. A Stornorechnung only exists when an original invoice
        // was already issued.
        if (original is null)
            return null;

        var remaining = await GetRemainingReversibleAmountAsync(original, cancellationToken);
        var stornoAmount = decimal.Round(Math.Min(Math.Abs(amount), remaining), 2);
        if (stornoAmount <= 0m)
            return null;

        var settings = await db.AccountingSettings.FirstOrDefaultAsync(
            x => x.TenantId == tenantId,
            cancellationToken);

        var invoiceNumber = await numbers.NextAsync(tenantId, refundDateUtc.Year, "ST", cancellationToken);
        var invoice = Invoice.Create(
            tenantId,
            InvoiceType.Sales,
            invoiceNumber,
            original.CustomerName ?? order.CustomerName,
            stornoAmount,
            original.Currency,
            refundDateUtc,
            $"Stornorechnung zu {original.InvoiceNumber}, Shopify Refund {refundId}.",
            order.Id,
            original.TaxRate,
            original.TaxType,
            InvoiceDocumentType.Storno,
            original.Id,
            refundId,
            order.ShopifyOrderId,
            original.CustomerEmail,
            original.CustomerAddress);

        db.Invoices.Add(invoice);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            var concurrent = await db.Invoices.FirstOrDefaultAsync(
                x => x.TenantId == tenantId && x.RefundId == refundId,
                cancellationToken);
            if (concurrent is not null)
                return concurrent;
            throw;
        }

        invoice.AttachPdf(pdf.Generate(invoice, settings ?? AccountingSettings.CreateDefault(tenantId)));
        await db.SaveChangesAsync(cancellationToken);
        return invoice;
    }

    public async Task<Invoice?> EnsureCancellationStornoInvoiceAsync(
        Guid tenantId,
        Order order,
        string cancellationEventId,
        DateTime cancellationDateUtc,
        CancellationToken cancellationToken)
    {
        if (tenantId == Guid.Empty || order.TenantId != tenantId || string.IsNullOrWhiteSpace(cancellationEventId))
            return null;

        var cancellationKey = $"CANCELLED:{cancellationEventId}";
        var existing = await db.Invoices.FirstOrDefaultAsync(
            x => x.TenantId == tenantId && x.RefundId == cancellationKey,
            cancellationToken);
        if (existing is not null)
            return existing;

        var original = await db.Invoices.FirstOrDefaultAsync(
            x => x.TenantId == tenantId &&
                 x.OrderId == order.Id &&
                 x.DocumentType == InvoiceDocumentType.Invoice,
            cancellationToken);

        if (original is null)
            return null;

        var remaining = await GetRemainingReversibleAmountAsync(original, cancellationToken);
        if (remaining <= 0m)
            return null;

        var settings = await db.AccountingSettings.FirstOrDefaultAsync(
            x => x.TenantId == tenantId,
            cancellationToken);

        var invoiceNumber = await numbers.NextAsync(tenantId, cancellationDateUtc.Year, "ST", cancellationToken);
        var invoice = Invoice.Create(
            tenantId,
            InvoiceType.Sales,
            invoiceNumber,
            original.CustomerName ?? order.CustomerName,
            remaining,
            original.Currency,
            cancellationDateUtc,
            $"Stornorechnung zu {original.InvoiceNumber}, Shopify Cancellation {cancellationEventId}.",
            order.Id,
            original.TaxRate,
            original.TaxType,
            InvoiceDocumentType.Storno,
            original.Id,
            cancellationKey,
            order.ShopifyOrderId,
            original.CustomerEmail,
            original.CustomerAddress);

        db.Invoices.Add(invoice);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            var concurrent = await db.Invoices.FirstOrDefaultAsync(
                x => x.TenantId == tenantId && x.RefundId == cancellationKey,
                cancellationToken);
            if (concurrent is not null)
                return concurrent;
            throw;
        }

        invoice.AttachPdf(pdf.Generate(invoice, settings ?? AccountingSettings.CreateDefault(tenantId)));
        await db.SaveChangesAsync(cancellationToken);
        return invoice;
    }

    private async Task<decimal> GetRemainingReversibleAmountAsync(
        Invoice original,
        CancellationToken cancellationToken)
    {
        var reversed = await db.Invoices
            .Where(x => x.TenantId == original.TenantId &&
                        x.OriginalInvoiceId == original.Id &&
                        x.DocumentType == InvoiceDocumentType.Storno)
            .Select(x => (decimal?)x.GrossAmount)
            .SumAsync(cancellationToken) ?? 0m;

        var remaining = original.GrossAmount + reversed; // Storno amounts are negative.
        return decimal.Round(Math.Max(0m, remaining), 2);
    }

}
