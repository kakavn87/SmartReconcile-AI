using SmartReconcile.Application.Abstractions;
using SmartReconcile.Application.DTOs;

namespace SmartReconcile.Application.Services;

public sealed class AccountingSettingsService(IAccountingSettingsRepository repository) : IAccountingSettingsService
{
    public async Task<AccountingSettingsDto> GetAsync(CancellationToken cancellationToken)
    {
        var settings = await repository.GetOrCreateForActiveTenantAsync(cancellationToken);
        return Map(settings);
    }

    public async Task<AccountingSettingsDto> UpdateAsync(UpdateAccountingSettingsRequest request, CancellationToken cancellationToken)
    {
        var settings = await repository.GetOrCreateForActiveTenantAsync(cancellationToken);

        settings.Update(
            request.Beraternummer,
            request.Mandantennummer,
            request.ChartOfAccounts,
            request.RevenueAccount19,
            request.RevenueAccount7,
            request.PaymentClearingAccount,
            request.FeeAccount);

        await repository.SaveAsync(cancellationToken);
        return Map(settings);
    }

    private static AccountingSettingsDto Map(SmartReconcile.Domain.Entities.AccountingSettings settings)
        => new(
            settings.Id,
            settings.TenantId,
            settings.Beraternummer,
            settings.Mandantennummer,
            settings.ChartOfAccounts,
            settings.RevenueAccount19,
            settings.RevenueAccount7,
            settings.PaymentClearingAccount,
            settings.FeeAccount,
            settings.UpdatedAtUtc);
}
