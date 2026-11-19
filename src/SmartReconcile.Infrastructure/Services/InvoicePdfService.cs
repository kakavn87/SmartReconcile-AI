using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SmartReconcile.Domain.Entities;

namespace SmartReconcile.Infrastructure.Services;

public sealed class InvoicePdfService
{
    public byte[] Generate(Invoice invoice, AccountingSettings settings)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var gross = invoice.GrossAmount;
        var net = invoice.NetAmount;
        var tax = invoice.TaxAmount;

        return Document.Create(container => container.Page(page =>
        {
            page.Margin(42);
            page.DefaultTextStyle(x => x.FontSize(10));
            page.Header().Column(column =>
            {
                column.Item().Text(settings.CompanyName.Length > 0 ? settings.CompanyName : "Unternehmen")
                    .FontSize(18).Bold();
                column.Item().Text(settings.CompanyAddress.Length > 0 ? settings.CompanyAddress : "Firmenanschrift nicht hinterlegt");
            });
            page.Content().PaddingTop(24).Column(column =>
            {
                column.Spacing(10);
                column.Item().Text(invoice.DocumentType == Domain.Enums.InvoiceDocumentType.Storno ? "STORNORECHNUNG" : "RECHNUNG")
                    .FontSize(20).Bold();
                column.Item().Text($"Rechnungsnummer: {invoice.InvoiceNumber}");
                column.Item().Text($"Rechnungsdatum: {invoice.InvoiceDateUtc.ToString("dd.MM.yyyy", CultureInfo.GetCultureInfo("de-DE"))}");
                if (!string.IsNullOrWhiteSpace(invoice.ShopifyOrderId))
                    column.Item().Text($"Shopify-Bestellung: {invoice.ShopifyOrderId}");
                column.Item().Text(invoice.CustomerName ?? invoice.SupplierName);
                if (!string.IsNullOrWhiteSpace(invoice.CustomerAddress))
                    column.Item().Text(invoice.CustomerAddress);
                if (!string.IsNullOrWhiteSpace(invoice.CustomerEmail))
                    column.Item().Text(invoice.CustomerEmail);

                column.Item().PaddingTop(12).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(3);
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });
                    table.Header(header =>
                    {
                        header.Cell().Text("Leistung").Bold();
                        header.Cell().AlignRight().Text("Netto").Bold();
                        header.Cell().AlignRight().Text("USt").Bold();
                        header.Cell().AlignRight().Text("Brutto").Bold();
                    });
                    table.Cell().Text("Shopify-Bestellung / E-Commerce-Leistung");
                    table.Cell().AlignRight().Text(Format(net, invoice.Currency));
                    table.Cell().AlignRight().Text($"{Format(tax, invoice.Currency)} ({invoice.TaxRate:0.##} %)");
                    table.Cell().AlignRight().Text(Format(gross, invoice.Currency));
                });

                column.Item().PaddingTop(16).AlignRight().Text($"Netto: {Format(net, invoice.Currency)}").Bold();
                column.Item().AlignRight().Text($"USt: {Format(tax, invoice.Currency)}");
                column.Item().AlignRight().Text($"Brutto: {Format(gross, invoice.Currency)}").FontSize(12).Bold();

                if (!string.IsNullOrWhiteSpace(settings.TaxNumber))
                    column.Item().PaddingTop(12).Text($"Steuernummer: {settings.TaxNumber}");
                if (!string.IsNullOrWhiteSpace(settings.VatId))
                    column.Item().Text($"USt-IdNr.: {settings.VatId}");
                if (!string.IsNullOrWhiteSpace(settings.BankDetails))
                    column.Item().PaddingTop(8).Text($"Bankverbindung: {settings.BankDetails}");

                if (string.IsNullOrWhiteSpace(invoice.CustomerAddress))
                    column.Item().PaddingTop(16).Text("Hinweis: Eine vollständige Rechnungsanschrift des Leistungsempfängers liegt im verfügbaren Shopify-Datensatz nicht vor.")
                        .Italic();
            });
            page.Footer().AlignCenter().Text($"SmartReconcile · {invoice.InvoiceNumber}");
        })).GeneratePdf();
    }

    private static string Format(decimal value, string currency)
        => $"{value.ToString("N2", CultureInfo.GetCultureInfo("de-DE"))} {currency}";
}
