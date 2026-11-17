using SmartReconcile.Application.Abstractions;
using SmartReconcile.Application.DTOs;
using SmartReconcile.Domain.Enums;

namespace SmartReconcile.Application.Services;

public sealed class DiscrepancyService(IDiscrepancyRepository repository) : IDiscrepancyService
{
    public async Task<IReadOnlyList<DiscrepancyDto>> GetAllAsync(CancellationToken cancellationToken) => (await repository.GetAllAsync(cancellationToken)).Select(Map).ToList();
    public async Task<DiscrepancyDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => (await repository.GetByIdAsync(id, cancellationToken)) is { } x ? Map(x) : null;
    public async Task<DiscrepancyDto?> UpdateStatusAsync(Guid id, UpdateDiscrepancyStatusRequest request, CancellationToken cancellationToken)
    {
        var entity = await repository.GetByIdAsync(id, cancellationToken);
        if (entity is null) return null;
        if (request.Status == DiscrepancyStatus.Resolved) entity.Resolve();
        else if (request.Status == DiscrepancyStatus.Ignored) entity.Ignore();
        await repository.SaveAsync(cancellationToken);
        return Map(entity);
    }
    private static DiscrepancyDto Map(SmartReconcile.Domain.Entities.Discrepancy x) => new(x.Id, x.Type, x.Status, x.Title, x.Description, x.AffectedAmount, x.OrderId, x.PaymentId, x.InvoiceId, x.DetectedAtUtc, x.AiAnalysis);
}