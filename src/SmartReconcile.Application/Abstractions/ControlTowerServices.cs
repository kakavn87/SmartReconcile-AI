using SmartReconcile.Application.DTOs;
using SmartReconcile.Domain.Entities;
using SmartReconcile.Domain.Enums;

namespace SmartReconcile.Application.Abstractions;

public interface IFinancialCaseRepository
{
    Task<List<FinancialCase>> GetAllAsync(Guid tenantId, CancellationToken cancellationToken);
    Task<FinancialCase?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken cancellationToken);
    Task<bool> ExistsOpenForDiscrepancyAsync(Guid tenantId, Guid discrepancyId, CancellationToken cancellationToken);
    Task AddAsync(FinancialCase financialCase, CancellationToken cancellationToken);
    Task SaveAsync(CancellationToken cancellationToken);
    Task<int> CountOpenAsync(Guid tenantId, CancellationToken cancellationToken);
    Task<int> CountHighRiskOpenAsync(Guid tenantId, CancellationToken cancellationToken);
    Task<decimal> SumOpenAffectedAmountAsync(Guid tenantId, CancellationToken cancellationToken);
}

public interface IFinancialCaseLinkRepository
{
    Task<List<FinancialCaseLink>> GetForCaseAsync(Guid tenantId, Guid financialCaseId, CancellationToken cancellationToken);
    Task AddRangeAsync(IEnumerable<FinancialCaseLink> links, CancellationToken cancellationToken);
}

public interface ICaseAuditEventRepository
{
    Task<List<CaseAuditEvent>> GetForCaseAsync(Guid tenantId, Guid financialCaseId, CancellationToken cancellationToken);
    Task AddAsync(CaseAuditEvent auditEvent, CancellationToken cancellationToken);
    Task SaveAsync(CancellationToken cancellationToken);
}

public interface IFinancialGraphRepository
{
    Task<bool> ExistsAsync(Guid tenantId, FinancialNodeType fromType, Guid fromId, FinancialNodeType toType, Guid toId, FinancialRelationType relationType, CancellationToken cancellationToken);
    Task AddAsync(FinancialGraphRelation relation, CancellationToken cancellationToken);
    Task<List<FinancialGraphRelation>> GetForNodeAsync(Guid tenantId, FinancialNodeType nodeType, Guid entityId, CancellationToken cancellationToken);
    Task SaveAsync(CancellationToken cancellationToken);
}

public interface IControlTowerService
{
    Task<ControlTowerDto> GetOverviewAsync(Guid tenantId, CancellationToken cancellationToken);
    Task<FinancialCaseDto?> GetCaseAsync(Guid tenantId, Guid id, CancellationToken cancellationToken);
    Task<FinancialCaseDetailDto?> GetCaseDetailAsync(Guid tenantId, Guid id, CancellationToken cancellationToken);
    Task<FinancialCaseDto?> StartInvestigationAsync(Guid tenantId, Guid id, CancellationToken cancellationToken);
    Task<FinancialCaseDto?> AutoFixAsync(Guid tenantId, Guid id, CancellationToken cancellationToken);
    Task<FinancialCaseDto?> ResolveAsync(Guid tenantId, Guid id, string? rootCause, string? note, CancellationToken cancellationToken);
    Task<FinancialCaseDto?> IgnoreAsync(Guid tenantId, Guid id, CancellationToken cancellationToken);
    Task<List<FinancialGraphRelationDto>> GetGraphAsync(Guid tenantId, FinancialNodeType nodeType, Guid entityId, CancellationToken cancellationToken);
}
