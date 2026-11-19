using SmartReconcile.Domain.Enums;

namespace SmartReconcile.Application.DTOs;

public sealed record FinancialCaseDto(
    Guid Id,
    Guid? DiscrepancyId,
    FinancialCaseStatus Status,
    FinancialCaseSeverity Severity,
    string Title,
    string Description,
    string? RootCause,
    decimal AffectedAmount,
    string Currency,
    DateTime DetectedAtUtc,
    DateTime? ResolvedAtUtc,
    IReadOnlyList<FinancialCaseLinkDto> Links);

public sealed record FinancialCaseLinkDto(
    FinancialNodeType NodeType,
    Guid EntityId,
    string Role,
    string Label,
    decimal? Amount,
    string Currency);

public sealed record FinancialGraphRelationDto(
    Guid Id,
    FinancialNodeType FromNodeType,
    Guid FromEntityId,
    FinancialNodeType ToNodeType,
    Guid ToEntityId,
    FinancialRelationType RelationType);

public sealed record SystemHealthDto(
    Guid? ConnectionId,
    string Name,
    string Provider,
    string Status,
    string? Detail,
    DateTime? LastSeenAtUtc);

public sealed record ControlTowerDto(
    decimal FinancialIntegrityScore,
    string FinancialIntegrityExplanation,
    decimal TotalRevenue,
    decimal ExpectedCash,
    decimal ActualCash,
    decimal CashGap,
    int OpenCaseCount,
    int HighRiskCaseCount,
    decimal AffectedAmount,
    IReadOnlyList<SystemHealthDto> SystemHealth,
    IReadOnlyList<FinancialCaseDto> Cases);

public sealed record CaseAuditEventDto(
    Guid Id,
    CaseAuditEventType EventType,
    string Description,
    string Actor,
    DateTime CreatedAtUtc);

public sealed record FinancialGraphNodeDto(
    FinancialNodeType NodeType,
    Guid? EntityId,
    bool Exists,
    string Status,
    string? Identifier,
    decimal? Amount,
    string Currency,
    DateTime? CreatedAtUtc,
    string? Detail);

public sealed record RootCauseAnalysisDto(
    string? Analysis,
    string? RecommendedAction,
    bool IsAvailable,
    bool IsFallback);

public sealed record FinancialCaseDetailDto(
    Guid Id,
    string CaseNumber,
    Guid? DiscrepancyId,
    string Title,
    string Description,
    FinancialCaseSeverity Severity,
    FinancialCaseStatus Status,
    decimal AffectedAmount,
    string Currency,
    DateTime DetectedAtUtc,
    DateTime? ResolvedAtUtc,
    IReadOnlyList<CaseAuditEventDto> Timeline,
    IReadOnlyList<FinancialGraphNodeDto> GraphNodes,
    RootCauseAnalysisDto RootCauseAnalysis);
