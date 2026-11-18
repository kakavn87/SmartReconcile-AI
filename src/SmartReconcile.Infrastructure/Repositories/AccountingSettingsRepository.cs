using Microsoft.EntityFrameworkCore;
using SmartReconcile.Application.Abstractions;
using SmartReconcile.Domain.Entities;
using SmartReconcile.Infrastructure.Persistence;

namespace SmartReconcile.Infrastructure.Repositories;

public sealed class AccountingSettingsRepository(SmartReconcileDbContext db) : IAccountingSettingsRepository
{
    public async Task<AccountingSettings> GetOrCreateAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant id is required.", nameof(tenantId));

        var settings = await db.AccountingSettings
            .FirstOrDefaultAsync(x => x.TenantId == tenantId, cancellationToken);

        if (settings is not null)
            return settings;

        if (!await db.Tenants.AnyAsync(x => x.Id == tenantId && x.IsActive, cancellationToken))
            throw new KeyNotFoundException("Tenant not found or inactive.");

        settings = AccountingSettings.CreateDefault(tenantId);
        await db.AccountingSettings.AddAsync(settings, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return settings;
    }

    public Task SaveAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
