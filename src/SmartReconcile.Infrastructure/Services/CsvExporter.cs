using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using SmartReconcile.Application.Abstractions;
using SmartReconcile.Infrastructure.Persistence;

namespace SmartReconcile.Infrastructure.Services;

public sealed class CsvExporter(SmartReconcileDbContext db) : ICsvExporter
{
    public async Task<byte[]> ExportDiscrepanciesAsync(CancellationToken ct)
    {
        var items = await db.Discrepancies.AsNoTracking().OrderByDescending(x => x.DetectedAtUtc).ToListAsync(ct);
        using var stream = new MemoryStream();
        using var writer = new StreamWriter(stream, new UTF8Encoding(true), 1024, true);
        using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.GetCultureInfo("de-DE")));
        csv.WriteField("ID"); csv.WriteField("Typ"); csv.WriteField("Status"); csv.WriteField("Titel"); csv.WriteField("Beschreibung"); csv.WriteField("Betrag"); csv.WriteField("Erkannt"); csv.NextRecord();
        foreach (var x in items)
        {
            csv.WriteField(x.Id); csv.WriteField(x.Type); csv.WriteField(x.Status); csv.WriteField(x.Title); csv.WriteField(x.Description); csv.WriteField(x.AffectedAmount); csv.WriteField(x.DetectedAtUtc); csv.NextRecord();
        }
        await writer.FlushAsync(ct);
        return stream.ToArray();
    }
}