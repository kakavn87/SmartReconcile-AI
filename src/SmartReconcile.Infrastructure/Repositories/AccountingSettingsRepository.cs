using Microsoft.EntityFrameworkCore;
using SmartReconcile.Application.Abstractions;
using SmartReconcile.Domain.Entities;
using SmartReconcile.Infrastructure.Persistence;

namespace SmartReconcile.Infrastructure.Repositories;

public sealed class AccountingSettingsRepository(SmartReconcileDbContext db) : IAccountingSettingsRepository
{
    public async Task<AccountingSettings> GetOrCreateForActiveTenantAsync(CancellationToken cancellationToken)
    {
        var tenant = await db.Tenants
            .Where(x => x.IsActive)
            .OrderBy(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (tenant is null)
        {
            tenant = Tenant.Create("Demo Unternehmen", "demo");
            await db.Tenants.AddAsync(tenant, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
        }

        var settings = await db.AccountingSettings
            .FirstOrDefaultAsync(x => x.TenantId == tenant.Id, cancellationToken);

        if (settings is not null)
            return settings;

        settings = AccountingSettings.CreateDefault(tenant.Id);
        await db.AccountingSettings.AddAsync(settings, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return settings;
    }

    public Task SaveAsync(CancellationToken cancellationToken)
        => db.SaveChangesAsync(cancellationToken);
}
