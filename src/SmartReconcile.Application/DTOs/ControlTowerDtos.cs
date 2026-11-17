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
    decimal TotalRevenue,
    decimal ExpectedCash,
    decimal ActualCash,
    decimal CashGap,
    int OpenCaseCount,
    int HighRiskCaseCount,
    decimal AffectedAmount,
    IReadOnlyList<SystemHealthDto> SystemHealth,
    IReadOnlyList<FinancialCaseDto> Cases);
