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
}