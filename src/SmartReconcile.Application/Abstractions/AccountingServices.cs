using SmartReconcile.Application.DTOs;

namespace SmartReconcile.Application.Abstractions;

public interface IAccountingSettingsService
{
    Task<AccountingSettingsDto> GetAsync(CancellationToken cancellationToken);
    Task<AccountingSettingsDto> UpdateAsync(UpdateAccountingSettingsRequest request, CancellationToken cancellationToken);
}

public interface IAccountingSettingsRepository
{
    Task<SmartReconcile.Domain.Entities.AccountingSettings> GetOrCreateForActiveTenantAsync(CancellationToken cancellationToken);
    Task SaveAsync(CancellationToken cancellationToken);
}

public interface IDatevExtfExporter
{
    Task<byte[]> ExportAsync(DateOnly from, DateOnly to, bool onlyReconciled, CancellationToken cancellationToken);
}
