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

        var settings = await db.AccountingSettings.FirstOrDefaultAsync(x => x.TenantId == tenantId, cancellationToken);
        if (settings is not null && !settings.AutomaticInvoiceCreationEnabled)
            return null;

        var existing = await db.Invoices.FirstOrDefaultAsync(
            x => x.TenantId == tenantId && x.RefundId == refundId,
            cancellationToken);
        if (existing is not null)
            return existing;

        var original = await db.Invoices.FirstOrDefaultAsync(
            x => x.TenantId == tenantId && x.OrderId == order.Id && x.DocumentType == InvoiceDocumentType.Invoice,
            cancellationToken);

        if (original is null)
            original = await EnsureSalesInvoiceAsync(tenantId, order.ShopifyOrderId ?? order.OrderNumber, order, cancellationToken);

        if (original is null)
            return null;

        var invoiceNumber = await numbers.NextAsync(tenantId, refundDateUtc.Year, "ST", cancellationToken);
        var invoice = Invoice.Create(
            tenantId,
            InvoiceType.Sales,
            invoiceNumber,
            original.CustomerName ?? order.CustomerName,
            amount,
            currency,
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
}
