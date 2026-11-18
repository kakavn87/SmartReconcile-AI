namespace SmartReconcile.Application.DTOs;

public sealed record AiAnalysisRequest(string DocumentText, string? Context);
public sealed record AiAnalysisResponse(string Analysis, bool UsedFallback);
