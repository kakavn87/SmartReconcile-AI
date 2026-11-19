using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SmartReconcile.Application.Abstractions;
using SmartReconcile.Application.DTOs;
using SmartReconcile.Domain.Entities;
using SmartReconcile.Domain.Enums;
using SmartReconcile.Infrastructure.Persistence;

namespace SmartReconcile.Infrastructure.Services;

public sealed class DatevExtfExporter(SmartReconcileDbContext db, IAccountingSettingsService accountingSettings) : IDatevExtfExporter
{
    private const string BookingHeader =
        "Umsatz (ohne Soll/Haben-Kz);Soll/Haben-Kennzeichen;WKZ Umsatz;Kurs;Basis-Umsatz;WKZ Basis-Umsatz;Konto;Gegenkonto (ohne BU-Schlüssel);BU-Schlüssel;Belegdatum;Belegfeld 1;Belegfeld 2;Skonto;Buchungstext;Postensperre;Diverse Adressnummer;Geschäftspartnerbank;Sachverhalt;Zinssperre;Beleglink;Beleginfo - Art 1;Beleginfo - Inhalt 1;Beleginfo - Art 2;Beleginfo - Inhalt 2;Beleginfo - Art 3;Beleginfo - Inhalt 3;Beleginfo - Art 4;Beleginfo - Inhalt 4;Beleginfo - Art 5;Beleginfo - Inhalt 5;Beleginfo - Art 6;Beleginfo - Inhalt 6;Beleginfo - Art 7;Beleginfo - Inhalt 7;Beleginfo - Art 8;Beleginfo - Inhalt 8;KOST1 - Kostenstelle;KOST2 - Kostenstelle;Kost-Menge;EU-Land u. UStID (Bestimmung);EU-Steuersatz (Bestimmung);Abw. Versteuerungsart;Sachverhalt L+L;Funktionsergänzung L+L;BU 49 Hauptfunktionstyp;BU 49 Hauptfunktionsnummer;BU 49 Funktionsergänzung;Zusatzinformation - Art 1;Zusatzinformation- Inhalt 1;Zusatzinformation - Art 2;Zusatzinformation- Inhalt 2;Zusatzinformation - Art 3;Zusatzinformation- Inhalt 3;Zusatzinformation - Art 4;Zusatzinformation- Inhalt 4;Zusatzinformation - Art 5;Zusatzinformation- Inhalt 5;Zusatzinformation - Art 6;Zusatzinformation- Inhalt 6;Zusatzinformation - Art 7;Zusatzinformation- Inhalt 7;Zusatzinformation - Art 8;Zusatzinformation- Inhalt 8;Zusatzinformation - Art 9;Zusatzinformation- Inhalt 9;Zusatzinformation - Art 10;Zusatzinformation- Inhalt 10;Zusatzinformation - Art 11;Zusatzinformation- Inhalt 11;Zusatzinformation - Art 12;Zusatzinformation- Inhalt 12;Zusatzinformation - Art 13;Zusatzinformation- Inhalt 13;Zusatzinformation - Art 14;Zusatzinformation- Inhalt 14;Zusatzinformation - Art 15;Zusatzinformation- Inhalt 15;Zusatzinformation - Art 16;Zusatzinformation- Inhalt 16;Zusatzinformation - Art 17;Zusatzinformation- Inhalt 17;Zusatzinformation - Art 18;Zusatzinformation- Inhalt 18;Zusatzinformation - Art 19;Zusatzinformation- Inhalt 19;Zusatzinformation - Art 20;Zusatzinformation- Inhalt 20;Stück;Gewicht;Zahlweise;Forderungsart;Veranlagungsjahr;Zugeordnete Fälligkeit;Skontotyp;Auftragsnummer;Buchungstyp;USt-Schlüssel (Anzahlungen);EU-Land (Anzahlungen);Sachverhalt L+L (Anzahlungen);EU-Steuersatz (Anzahlungen);Erlöskonto (Anzahlungen);Herkunft-Kz;Buchungs GUID;KOST-Datum;SEPA-Mandatsreferenz;Skontosperre;Gesellschaftername;Beteiligtennummer;Identifikationsnummer;Zeichnernummer;Postensperre bis;Bezeichnung SoBil-Sachverhalt;Kennzeichen SoBil-Buchung;Festschreibung;Leistungsdatum;Datum Zuord. Steuerperiode;Fälligkeit;Generalumkehr (GU);Steuersatz;Land;Abrechnungsreferenz;BVV-Position;EU-Land u. UStID (Ursprung);EU-Steuersatz (Ursprung);Abw. Skontokonto";

    private static readonly string[] BookingColumns = BookingHeader.Split(';');

    public async Task<byte[]> ExportAsync(Guid tenantId, DateOnly from, DateOnly to, bool onlyReconciled, CancellationToken cancellationToken)
    {
        if (to < from)
            throw new ArgumentException("Das Enddatum darf nicht vor dem Startdatum liegen.");

        var settings = await accountingSettings.GetAsync(tenantId, cancellationToken);
        var fromUtc = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toExclusiveUtc = to.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        var orders = await db.Orders
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.CreatedAtUtc >= fromUtc && x.CreatedAtUtc < toExclusiveUtc)
            .OrderBy(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var orderIds = orders.Select(x => x.Id).ToArray();
        var payments = await db.Payments
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && orderIds.Contains(x.OrderId))
            .ToListAsync(cancellationToken);

        var paymentIds = payments.Select(x => x.Id).ToArray();
        var openDiscrepancies = await db.Discrepancies
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Status == DiscrepancyStatus.Open &&
                        ((x.OrderId.HasValue && orderIds.Contains(x.OrderId.Value)) ||
                         (x.PaymentId.HasValue && paymentIds.Contains(x.PaymentId.Value))))
            .ToListAsync(cancellationToken);

        var reconciledOrderIds = orders
            .Where(order =>
            {
                var payment = payments.FirstOrDefault(x =>
                    x.OrderId == order.Id &&
                    x.Kind != PaymentKind.Refund &&
                    x.Status == PaymentStatus.Succeeded);

                var hasOpenDiscrepancy = openDiscrepancies.Any(x =>
                    x.OrderId == order.Id ||
                    (payment is not null && x.PaymentId == payment.Id));

                return payment is not null &&
                       Math.Abs(order.TotalAmount - payment.Amount) <= 0.01m &&
                       !hasOpenDiscrepancy;
            })
            .Select(x => x.Id)
            .ToHashSet();

        // DATEV export records accounting-relevant paid orders. A partial refund
        // is represented by an Order that remains Paid plus a separate Refund
        // payment; a fully refunded order is represented by OrderStatus.Refunded.
        // Discrepancies do not exclude orders unless the caller explicitly asks
        // for onlyReconciled.
        var selectedOrders = onlyReconciled
            ? orders.Where(x => reconciledOrderIds.Contains(x.Id)).ToList()
            : orders.Where(x => x.Status is OrderStatus.Paid or OrderStatus.Refunded).ToList();

        var selectedOrderIds = selectedOrders.Select(x => x.Id).ToHashSet();
        var selectedPayments = payments
            .Where(x =>
                selectedOrderIds.Contains(x.OrderId) &&
                (x.Status == PaymentStatus.Succeeded || x.Status == PaymentStatus.Refunded))
            .OrderBy(x => x.PaidAtUtc)
            .ToList();

        var bookings = new List<DatevBooking>();

        foreach (var order in selectedOrders)
        {
            var revenueAccount = order.TaxRate <= 7.01m
                ? settings.RevenueAccount7
                : settings.RevenueAccount19;

            bookings.Add(DatevBooking.Revenue(
                order.TotalAmount,
                revenueAccount,
                settings.PaymentClearingAccount,
                order.CreatedAtUtc,
                order.OrderNumber,
                $"E-Commerce Umsatz {order.OrderNumber}"));
        }

        // Refunds reverse the original clearing/revenue posting:
        // clearing account remains Konto, revenue account remains Gegenkonto,
        // but the Konto is credited (H) instead of debited (S).
        foreach (var refund in selectedPayments.Where(x => x.Kind == PaymentKind.Refund && x.Amount > 0))
        {
            var order = selectedOrders.FirstOrDefault(x => x.Id == refund.OrderId);
            if (order is null)
                continue;

            var revenueAccount = order.TaxRate <= 7.01m
                ? settings.RevenueAccount7
                : settings.RevenueAccount19;

            bookings.Add(DatevBooking.Refund(
                refund.Amount,
                revenueAccount,
                settings.PaymentClearingAccount,
                refund.PaidAtUtc,
                refund.TransactionId,
                $"Erstattung {order.OrderNumber}"));
        }

        foreach (var payment in selectedPayments.Where(x => x.FeeAmount > 0))
        {
            bookings.Add(DatevBooking.Fee(
                payment.FeeAmount,
                settings.FeeAccount,
                settings.PaymentClearingAccount,
                payment.PaidAtUtc,
                payment.TransactionId,
                $"Zahlungsgebühr {payment.TransactionId}"));
        }

        return BuildFile(settings, from, to, bookings);
    }

    private static byte[] BuildFile(
        AccountingSettingsDto settings,
        DateOnly from,
        DateOnly to,
        IReadOnlyCollection<DatevBooking> bookings)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var encoding = Encoding.GetEncoding(1252);
        var generatedAt = DateTime.Now;
        var fiscalYearStart = new DateOnly(from.Year, 1, 1);
        var skr = settings.ChartOfAccounts == ChartOfAccounts.SKR03 ? "03" : "04";

        var header = string.Join(";",
            Quote("EXTF"), "700", "21", Quote("Buchungsstapel"), "13",
            generatedAt.ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture),
            "",
            Quote("SR"),
            Quote("SmartReconcile"),
            Quote(""),
            settings.Beraternummer,
            settings.Mandantennummer,
            fiscalYearStart.ToString("yyyyMMdd", CultureInfo.InvariantCulture),
            "4",
            from.ToString("yyyyMMdd", CultureInfo.InvariantCulture),
            to.ToString("yyyyMMdd", CultureInfo.InvariantCulture),
            Quote($"SmartReconcile {to:MM.yyyy}"),
            Quote("SR"),
            "1",
            "0",
            "0",
            Quote("EUR"),
            "",
            Quote(""),
            "",
            "",
            Quote(skr),
            "",
            "",
            Quote(""),
            Quote(""));

        var lines = new List<string> { header, BookingHeader };
        lines.AddRange(bookings.Select(ToCsvLine));

        var content = string.Join("\r\n", lines) + "\r\n";
        return encoding.GetBytes(content);
    }

    private static string ToCsvLine(DatevBooking booking)
    {
        var fields = new string[BookingColumns.Length];
        Array.Fill(fields, "");

        fields[0] = FormatAmount(booking.Amount);
        fields[1] = booking.Debit ? "S" : "H";
        fields[2] = Quote("");
        fields[6] = booking.Account;
        fields[7] = booking.CounterAccount;
        fields[8] = Quote("");
        fields[9] = booking.DocumentDate.ToString("ddMM", CultureInfo.InvariantCulture);
        fields[10] = Quote(booking.DocumentReference);
        fields[13] = Quote(booking.Text);
        fields[94] = Quote(booking.DocumentReference);

        return string.Join(";", fields);
    }

    private static string FormatAmount(decimal amount)
        => amount.ToString("0.00", CultureInfo.GetCultureInfo("de-DE"));

    private static string Quote(string value)
    {
        var sanitized = value
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal)
            .Replace("\"", "\"\"", StringComparison.Ordinal);

        return $"\"{sanitized}\"";
    }

    private sealed record DatevBooking(
        decimal Amount,
        bool Debit,
        string Account,
        string CounterAccount,
        DateTime DocumentDate,
        string DocumentReference,
        string Text)
    {
        public static DatevBooking Revenue(
            decimal amount,
            string revenueAccount,
            string clearingAccount,
            DateTime date,
            string reference,
            string text)
            // DATEV convention used here: clearing/receivable account in Konto,
            // revenue account in Gegenkonto, Konto is debited.
            => new(amount, true, clearingAccount, revenueAccount, date, reference, text);

        public static DatevBooking Refund(
            decimal amount,
            string revenueAccount,
            string clearingAccount,
            DateTime date,
            string reference,
            string text)
            // Reverse the original posting: clearing/receivable account in Konto,
            // revenue account in Gegenkonto, Konto is credited.
            => new(amount, false, clearingAccount, revenueAccount, date, reference, text);

        public static DatevBooking Fee(
            decimal amount,
            string feeAccount,
            string clearingAccount,
            DateTime date,
            string reference,
            string text)
            // Fee expense is debited against the clearing account.
            => new(amount, true, feeAccount, clearingAccount, date, reference, text);
    }
}
