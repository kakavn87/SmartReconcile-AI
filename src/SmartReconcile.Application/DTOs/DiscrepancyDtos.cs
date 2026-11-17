using SmartReconcile.Domain.Enums;

namespace SmartReconcile.Application.DTOs;

public sealed record DiscrepancyDto(Guid Id, DiscrepancyType Type, DiscrepancyStatus Status, string Title, string Description, decimal AffectedAmount, Guid? OrderId, Guid? PaymentId, Guid? InvoiceId, DateTime DetectedAtUtc, string? AiAnalysis);
public sealed record UpdateDiscrepancyStatusRequest(DiscrepancyStatus Status);
public sealed record ReconciliationResult(int CreatedCount, IReadOnlyList<DiscrepancyDto> Discrepancies);
public sealed record DashboardDto(decimal TotalRevenue, int OpenDiscrepancyCount, decimal TotalAffectedAmount, int OrderCount, int PaymentCount, int InvoiceCount);