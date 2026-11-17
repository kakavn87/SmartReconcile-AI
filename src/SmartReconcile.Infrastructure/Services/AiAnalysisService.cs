using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using SmartReconcile.Application.Abstractions;
using SmartReconcile.Application.DTOs;

namespace SmartReconcile.Infrastructure.Services;

public sealed class AiAnalysisService(HttpClient httpClient, IConfiguration configuration) : IAiAnalysisService
{
    public async Task<AiAnalysisResponse> AnalyzeInvoiceAsync(AiAnalysisRequest request, CancellationToken cancellationToken)
    {
        var apiKey = configuration["OpenAI:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            return new AiAnalysisResponse(BuildFallback(request), true);

        var model = configuration["OpenAI:Model"] ?? "gpt-5-mini";
        try
        {
            using var message = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/responses");
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            message.Content = JsonContent.Create(new
            {
                model,
                input = $"Analysiere den folgenden Rechnungstext für die deutsche Buchhaltung. Erkläre kurz und verständlich, ob Auffälligkeiten oder Widersprüche vorliegen. Kontext: {request.Context ?? "keiner"}. Rechnung: {request.DocumentText}"
            });
            using var response = await httpClient.SendAsync(message, cancellationToken);
            if (!response.IsSuccessStatusCode) return new AiAnalysisResponse(BuildFallback(request), true);
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            var text = ExtractOutputText(document.RootElement);
            return new AiAnalysisResponse(string.IsNullOrWhiteSpace(text) ? BuildFallback(request) : text, string.IsNullOrWhiteSpace(text));
        }
        catch
        {
            return new AiAnalysisResponse(BuildFallback(request), true);
        }
    }

    public async Task<FinancialCaseAnalysisResponse> AnalyzeFinancialCaseAsync(
        FinancialCaseAnalysisRequest request,
        CancellationToken cancellationToken)
    {
        var apiKey = configuration["OpenAI:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            return BuildFinancialCaseFallback(request);

        var model = configuration["OpenAI:Model"] ?? "gpt-5-mini";
        try
        {
            using var message = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/responses");
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            message.Content = JsonContent.Create(new
            {
                model,
                input = $"""
                    Du bist ein KI-Prüfassistent für die deutsche Finanzbuchhaltung.
                    Analysiere den folgenden Finanzfall präzise und in deutscher Sprache.

                    Falltitel: {request.Title}
                    Beschreibung: {request.Description}
                    Kontext: {request.Context}

                    Antworte exakt in diesem Format:
                    URSACHE: <kurze Root-Cause-Erklärung>
                    EMPFEHLUNG: <konkrete empfohlene Korrekturmaßnahme>

                    Keine Einleitung, keine Markdown-Überschriften und keine zusätzlichen Zeilen.
                    """
            });

            using var response = await httpClient.SendAsync(message, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return BuildFinancialCaseFallback(request);

            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            var text = ExtractOutputText(document.RootElement);
            if (string.IsNullOrWhiteSpace(text))
                return BuildFinancialCaseFallback(request);

            var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var analysis = lines.FirstOrDefault(x => x.StartsWith("URSACHE:", StringComparison.OrdinalIgnoreCase))?
                ["URSACHE:".Length..].Trim();
            var recommendation = lines.FirstOrDefault(x => x.StartsWith("EMPFEHLUNG:", StringComparison.OrdinalIgnoreCase))?
                ["EMPFEHLUNG:".Length..].Trim();

            if (string.IsNullOrWhiteSpace(analysis) || string.IsNullOrWhiteSpace(recommendation))
                return new FinancialCaseAnalysisResponse(text.Trim(), "Abweichung prüfen und den betroffenen Finanzbeleg mit den Zahlungs- und Buchungsdaten abgleichen.", false);

            return new FinancialCaseAnalysisResponse(analysis, recommendation, false);
        }
        catch
        {
            return BuildFinancialCaseFallback(request);
        }
    }

    private static string ExtractOutputText(JsonElement root)
    {
        if (root.TryGetProperty("output_text", out var direct) && direct.ValueKind == JsonValueKind.String) return direct.GetString() ?? string.Empty;
        if (!root.TryGetProperty("output", out var output) || output.ValueKind != JsonValueKind.Array) return string.Empty;
        foreach (var item in output.EnumerateArray())
        {
            if (!item.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array) continue;
            foreach (var part in content.EnumerateArray())
                if (part.TryGetProperty("text", out var text) && text.ValueKind == JsonValueKind.String) return text.GetString() ?? string.Empty;
        }
        return string.Empty;
    }

    private static string BuildFallback(AiAnalysisRequest request)
    {
        var lower = request.DocumentText.ToLowerInvariant();
        if (lower.Contains("storno") || lower.Contains("gutschrift")) return "Hinweis: Der Rechnungstext enthält einen möglichen Storno- oder Gutschrift-Hinweis. Bitte Betrag und Referenzbeleg prüfen.";
        if (lower.Contains("mwst") || lower.Contains("umsatzsteuer")) return "Die Rechnung enthält Steuerangaben. Bitte Rechnungsbetrag, Nettobetrag und Umsatzsteuer gegen den Beleg abgleichen.";
        return "Automatische Analyse im Demo-Fallback: Keine eindeutige Auffälligkeit aus dem bereitgestellten Rechnungstext erkennbar. Betrag, Rechnungsnummer und Lieferant sollten mit den Buchungsdaten abgeglichen werden.";
    }

    private static FinancialCaseAnalysisResponse BuildFinancialCaseFallback(FinancialCaseAnalysisRequest request)
    {
        var text = $"{request.Title} {request.Description} {request.Context}".ToLowerInvariant();

        if (text.Contains("betrag") || text.Contains("amount"))
        {
            return new FinancialCaseAnalysisResponse(
                "Die Abweichung deutet auf eine Differenz zwischen den erfassten Finanzbeträgen hin. Die betroffenen Belege und Zahlungsdaten stimmen nicht vollständig überein.",
                "Zahlungsbetrag, Bestellbetrag und zugehörigen Beleg abgleichen und die Differenz anschließend buchhalterisch korrigieren.",
                true);
        }

        if (text.Contains("zahlung") || text.Contains("payment"))
        {
            return new FinancialCaseAnalysisResponse(
                "Der Finanzfall weist auf eine fehlende, verspätete oder nicht eindeutig zugeordnete Zahlung hin.",
                "Zahlung beim Zahlungsanbieter prüfen, Transaktionsreferenz abgleichen und die Zuordnung zur Bestellung nachvollziehen.",
                true);
        }

        if (text.Contains("rechnung") || text.Contains("invoice"))
        {
            return new FinancialCaseAnalysisResponse(
                "Der Finanzfall weist auf eine Unstimmigkeit im Zusammenhang mit einer Rechnung oder Rechnungsnummer hin.",
                "Rechnungsnummer, Lieferant und Betrag gegen die vorhandenen Buchungsdaten prüfen und einen möglichen Dublettenfall bereinigen.",
                true);
        }

        return new FinancialCaseAnalysisResponse(
            "Die verfügbaren Finanzdaten weisen auf eine Unstimmigkeit hin, die eine manuelle Prüfung der verknüpften Belege erfordert.",
            "Bestellung, Zahlung und Rechnung miteinander abgleichen und die festgestellte Abweichung dokumentiert korrigieren.",
            true);
    }
}
