using SmartReconcile.Application.DTOs;

namespace SmartReconcile.Application.Abstractions;

public interface IAccountingSettingsService
{
    Task<AccountingSettingsDto> GetAsync(Guid tenantId, CancellationToken cancellationToken);
    Task<AccountingSettingsDto> UpdateAsync(Guid tenantId, UpdateAccountingSettingsRequest request, CancellationToken cancellationToken);
}

public interface IAccountingSettingsRepository
{
    Task<SmartReconcile.Domain.Entities.AccountingSettings> GetOrCreateAsync(Guid tenantId, CancellationToken cancellationToken);
    Task SaveAsync(CancellationToken cancellationToken);
}

public interface IDatevExtfExporter
{
    Task<byte[]> ExportAsync(Guid tenantId, DateOnly from, DateOnly to, bool onlyReconciled, CancellationToken cancellationToken);
}
