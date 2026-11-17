namespace SmartReconcile.Application.DTOs;

public sealed record SeedResult(int OrdersCreated, int PaymentsCreated, int InvoicesCreated, int IntentionalIssuesCreated);
public sealed record AiAnalysisRequest(string DocumentText, string? Context);
public sealed record AiAnalysisResponse(string Analysis, bool UsedFallback);