using Microsoft.EntityFrameworkCore;
using SmartReconcile.Application.Abstractions;
using SmartReconcile.Domain.Entities;
using SmartReconcile.Domain.Enums;
using SmartReconcile.Infrastructure.Persistence;

namespace SmartReconcile.Infrastructure.Repositories;

public sealed class FinancialCaseRepository(SmartReconcileDbContext db) : IFinancialCaseRepository
{
    public Task<List<FinancialCase>> GetAllAsync(CancellationToken ct)
        => db.FinancialCases.AsNoTracking().OrderByDescending(x => x.DetectedAtUtc).ToListAsync(ct);

    public Task<FinancialCase?> GetByIdAsync(Guid id, CancellationToken ct)
        => db.FinancialCases.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<bool> ExistsOpenForDiscrepancyAsync(Guid discrepancyId, CancellationToken ct)
        => db.FinancialCases.AnyAsync(
            x => x.DiscrepancyId == discrepancyId &&
                 (x.Status == FinancialCaseStatus.Open || x.Status == FinancialCaseStatus.Investigating),
            ct);

    public async Task AddAsync(FinancialCase financialCase, CancellationToken ct)
        => await db.FinancialCases.AddAsync(financialCase, ct);

    public Task SaveAsync(CancellationToken ct) => db.SaveChangesAsync(ct);

    public Task<int> CountOpenAsync(CancellationToken ct)
        => db.FinancialCases.CountAsync(
            x => x.Status == FinancialCaseStatus.Open || x.Status == FinancialCaseStatus.Investigating,
            ct);

    public Task<int> CountHighRiskOpenAsync(CancellationToken ct)
        => db.FinancialCases.CountAsync(
            x => (x.Status == FinancialCaseStatus.Open || x.Status == FinancialCaseStatus.Investigating) &&
                 (x.Severity == FinancialCaseSeverity.High || x.Severity == FinancialCaseSeverity.Critical),
            ct);

    public Task<decimal> SumOpenAffectedAmountAsync(CancellationToken ct)
        => db.FinancialCases
            .Where(x => x.Status == FinancialCaseStatus.Open || x.Status == FinancialCaseStatus.Investigating)
            .SumAsync(x => x.AffectedAmount, ct);
}

public sealed class FinancialCaseLinkRepository(SmartReconcileDbContext db) : IFinancialCaseLinkRepository
{
    public Task<List<FinancialCaseLink>> GetForCaseAsync(Guid financialCaseId, CancellationToken ct)
        => db.FinancialCaseLinks.AsNoTracking().Where(x => x.FinancialCaseId == financialCaseId).ToListAsync(ct);

    public async Task AddRangeAsync(IEnumerable<FinancialCaseLink> links, CancellationToken ct)
        => await db.FinancialCaseLinks.AddRangeAsync(links, ct);
}

public sealed class FinancialGraphRepository(SmartReconcileDbContext db) : IFinancialGraphRepository
{
    public Task<bool> ExistsAsync(
        FinancialNodeType fromType,
        Guid fromId,
        FinancialNodeType toType,
        Guid toId,
        FinancialRelationType relationType,
        CancellationToken ct)
        => db.FinancialGraphRelations.AnyAsync(
            x => x.FromNodeType == fromType &&
                 x.FromEntityId == fromId &&
                 x.ToNodeType == toType &&
                 x.ToEntityId == toId &&
                 x.RelationType == relationType,
            ct);

    public async Task AddAsync(FinancialGraphRelation relation, CancellationToken ct)
        => await db.FinancialGraphRelations.AddAsync(relation, ct);

    public Task<List<FinancialGraphRelation>> GetForNodeAsync(
        FinancialNodeType nodeType,
        Guid entityId,
        CancellationToken ct)
        => db.FinancialGraphRelations.AsNoTracking()
            .Where(x => (x.FromNodeType == nodeType && x.FromEntityId == entityId) ||
                        (x.ToNodeType == nodeType && x.ToEntityId == entityId))
            .OrderBy(x => x.CreatedAtUtc)
            .ToListAsync(ct);

    public Task SaveAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
