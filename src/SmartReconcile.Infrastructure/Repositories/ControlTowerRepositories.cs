using Microsoft.EntityFrameworkCore;
using SmartReconcile.Application.Abstractions;
using SmartReconcile.Domain.Entities;
using SmartReconcile.Domain.Enums;
using SmartReconcile.Infrastructure.Persistence;

namespace SmartReconcile.Infrastructure.Repositories;

public sealed class FinancialCaseRepository(SmartReconcileDbContext db) : IFinancialCaseRepository
{
    public Task<List<FinancialCase>> GetAllAsync(Guid tenantId, CancellationToken ct)
        => db.FinancialCases.AsNoTracking().Where(x => x.TenantId == tenantId).OrderByDescending(x => x.DetectedAtUtc).ToListAsync(ct);

    public Task<FinancialCase?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct)
        => db.FinancialCases.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);

    public Task<bool> ExistsOpenForDiscrepancyAsync(Guid tenantId, Guid discrepancyId, CancellationToken ct)
        => db.FinancialCases.AnyAsync(x => x.TenantId == tenantId && x.DiscrepancyId == discrepancyId &&
            (x.Status == FinancialCaseStatus.Open || x.Status == FinancialCaseStatus.Investigating), ct);

    public async Task AddAsync(FinancialCase financialCase, CancellationToken ct) => await db.FinancialCases.AddAsync(financialCase, ct);
    public Task SaveAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
    public Task<int> CountOpenAsync(Guid tenantId, CancellationToken ct)
        => db.FinancialCases.CountAsync(x => x.TenantId == tenantId && (x.Status == FinancialCaseStatus.Open || x.Status == FinancialCaseStatus.Investigating), ct);
    public Task<int> CountHighRiskOpenAsync(Guid tenantId, CancellationToken ct)
        => db.FinancialCases.CountAsync(x => x.TenantId == tenantId &&
            (x.Status == FinancialCaseStatus.Open || x.Status == FinancialCaseStatus.Investigating) &&
            (x.Severity == FinancialCaseSeverity.High || x.Severity == FinancialCaseSeverity.Critical), ct);
    public Task<decimal> SumOpenAffectedAmountAsync(Guid tenantId, CancellationToken ct)
        => db.FinancialCases.Where(x => x.TenantId == tenantId &&
            (x.Status == FinancialCaseStatus.Open || x.Status == FinancialCaseStatus.Investigating)).SumAsync(x => x.AffectedAmount, ct);
}

public sealed class FinancialCaseLinkRepository(SmartReconcileDbContext db) : IFinancialCaseLinkRepository
{
    public Task<List<FinancialCaseLink>> GetForCaseAsync(Guid tenantId, Guid financialCaseId, CancellationToken ct)
        => db.FinancialCaseLinks.AsNoTracking().Where(x => x.TenantId == tenantId && x.FinancialCaseId == financialCaseId).ToListAsync(ct);
    public async Task AddRangeAsync(IEnumerable<FinancialCaseLink> links, CancellationToken ct) => await db.FinancialCaseLinks.AddRangeAsync(links, ct);
}

public sealed class CaseAuditEventRepository(SmartReconcileDbContext db) : ICaseAuditEventRepository
{
    public Task<List<CaseAuditEvent>> GetForCaseAsync(Guid tenantId, Guid financialCaseId, CancellationToken ct)
        => db.CaseAuditEvents.AsNoTracking().Where(x => x.TenantId == tenantId && x.FinancialCaseId == financialCaseId).OrderByDescending(x => x.CreatedAtUtc).ToListAsync(ct);
    public async Task AddAsync(CaseAuditEvent auditEvent, CancellationToken ct) => await db.CaseAuditEvents.AddAsync(auditEvent, ct);
    public Task SaveAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}

public sealed class FinancialGraphRepository(SmartReconcileDbContext db) : IFinancialGraphRepository
{
    public Task<bool> ExistsAsync(Guid tenantId, FinancialNodeType fromType, Guid fromId, FinancialNodeType toType, Guid toId, FinancialRelationType relationType, CancellationToken ct)
        => db.FinancialGraphRelations.AnyAsync(x => x.TenantId == tenantId && x.FromNodeType == fromType && x.FromEntityId == fromId && x.ToNodeType == toType && x.ToEntityId == toId && x.RelationType == relationType, ct);

    public async Task AddAsync(FinancialGraphRelation relation, CancellationToken ct) => await db.FinancialGraphRelations.AddAsync(relation, ct);

    public Task<List<FinancialGraphRelation>> GetForNodeAsync(Guid tenantId, FinancialNodeType nodeType, Guid entityId, CancellationToken ct)
        => db.FinancialGraphRelations.AsNoTracking().Where(x => x.TenantId == tenantId &&
            ((x.FromNodeType == nodeType && x.FromEntityId == entityId) || (x.ToNodeType == nodeType && x.ToEntityId == entityId)))
            .OrderBy(x => x.CreatedAtUtc).ToListAsync(ct);

    public Task SaveAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
