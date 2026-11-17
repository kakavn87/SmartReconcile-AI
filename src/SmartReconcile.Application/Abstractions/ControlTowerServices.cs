using SmartReconcile.Application.DTOs;
using SmartReconcile.Domain.Entities;
using SmartReconcile.Domain.Enums;

namespace SmartReconcile.Application.Abstractions;

public interface IFinancialCaseRepository
{
    Task<List<FinancialCase>> GetAllAsync(CancellationToken cancellationToken);
    Task<FinancialCase?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> ExistsOpenForDiscrepancyAsync(Guid discrepancyId, CancellationToken cancellationToken);
    Task AddAsync(FinancialCase financialCase, CancellationToken cancellationToken);
    Task SaveAsync(CancellationToken cancellationToken);
    Task<int> CountOpenAsync(CancellationToken cancellationToken);
    Task<int> CountHighRiskOpenAsync(CancellationToken cancellationToken);
    Task<decimal> SumOpenAffectedAmountAsync(CancellationToken cancellationToken);
}

public interface IFinancialCaseLinkRepository
{
    Task<List<FinancialCaseLink>> GetForCaseAsync(Guid financialCaseId, CancellationToken cancellationToken);
    Task AddRangeAsync(IEnumerable<FinancialCaseLink> links, CancellationToken cancellationToken);
}

public interface ICaseAuditEventRepository
{
    Task<List<CaseAuditEvent>> GetForCaseAsync(Guid financialCaseId, CancellationToken cancellationToken);
    Task AddAsync(CaseAuditEvent auditEvent, CancellationToken cancellationToken);
    Task SaveAsync(CancellationToken cancellationToken);
}

public interface IFinancialGraphRepository
{
    Task<bool> ExistsAsync(FinancialNodeType fromType, Guid fromId, FinancialNodeType toType, Guid toId, FinancialRelationType relationType, CancellationToken cancellationToken);
    Task AddAsync(FinancialGraphRelation relation, CancellationToken cancellationToken);
    Task<List<FinancialGraphRelation>> GetForNodeAsync(FinancialNodeType nodeType, Guid entityId, CancellationToken cancellationToken);
    Task SaveAsync(CancellationToken cancellationToken);
}

public interface IControlTowerService
{
    Task<ControlTowerDto> GetOverviewAsync(CancellationToken cancellationToken);
    Task<FinancialCaseDto?> GetCaseAsync(Guid id, CancellationToken cancellationToken);
    Task<FinancialCaseDetailDto?> GetCaseDetailAsync(Guid id, CancellationToken cancellationToken);
    Task<FinancialCaseDto?> StartInvestigationAsync(Guid id, CancellationToken cancellationToken);
    Task<FinancialCaseDto?> AutoFixAsync(Guid id, CancellationToken cancellationToken);
    Task<FinancialCaseDto?> ResolveAsync(Guid id, string? rootCause, string? note, CancellationToken cancellationToken);
    Task<FinancialCaseDto?> IgnoreAsync(Guid id, CancellationToken cancellationToken);
    Task<List<FinancialGraphRelationDto>> GetGraphAsync(FinancialNodeType nodeType, Guid entityId, CancellationToken cancellationToken);
}
