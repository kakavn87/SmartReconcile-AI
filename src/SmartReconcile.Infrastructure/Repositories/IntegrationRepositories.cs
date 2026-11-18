using Microsoft.EntityFrameworkCore;
using SmartReconcile.Application.Abstractions;
using SmartReconcile.Domain.Entities;
using SmartReconcile.Infrastructure.Persistence;

namespace SmartReconcile.Infrastructure.Repositories;

public sealed class TenantRepository(SmartReconcileDbContext db) : ITenantRepository
{
    public Task<Tenant?> GetByIdAsync(Guid id, CancellationToken ct)
        => db.Tenants.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<Tenant?> GetBySlugAsync(string slug, CancellationToken ct)
        => db.Tenants.FirstOrDefaultAsync(x => x.Slug == slug, ct);

    public Task<bool> ExistsBySlugAsync(string slug, CancellationToken ct)
        => db.Tenants.AnyAsync(x => x.Slug == slug, ct);

    public async Task AddAsync(Tenant tenant, CancellationToken ct)
    {
        await db.Tenants.AddAsync(tenant, ct);
        await db.SaveChangesAsync(ct);
    }
}

public sealed class IntegrationConnectionRepository(SmartReconcileDbContext db) : IIntegrationConnectionRepository
{
    public Task<IntegrationConnection?> GetByIdAsync(Guid id, CancellationToken ct)
        => db.IntegrationConnections.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<IntegrationConnection?> ResolveActiveByApiKeyHashAsync(string apiKeyHash, CancellationToken ct)
        => db.IntegrationConnections
            .FirstOrDefaultAsync(x => x.ApiKeyHash == apiKeyHash && (x.Status == SmartReconcile.Domain.Enums.ConnectionStatus.Active || x.Status == SmartReconcile.Domain.Enums.ConnectionStatus.Connected), ct);

    public Task<IntegrationConnection?> GetShopifyByDomainAsync(string shopDomain, CancellationToken ct)
        => db.IntegrationConnections
            .FirstOrDefaultAsync(
                x => x.Provider == SmartReconcile.Domain.Enums.IntegrationProvider.Shopify
                    && x.ShopDomain == shopDomain,
                ct);

    public Task<List<IntegrationConnection>> GetForTenantAsync(Guid tenantId, CancellationToken ct)
        => db.IntegrationConnections
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(ct);

    public Task<List<IntegrationConnection>> GetAllAsync(CancellationToken ct)
        => db.IntegrationConnections.AsNoTracking().OrderBy(x => x.Provider).ThenBy(x => x.DisplayName).ToListAsync(ct);

    public async Task AddAsync(IntegrationConnection connection, CancellationToken ct)
    {
        await db.IntegrationConnections.AddAsync(connection, ct);
        await db.SaveChangesAsync(ct);
    }

    public Task SaveAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}

public sealed class WebhookEventRepository(SmartReconcileDbContext db) : IWebhookEventRepository
{
    public Task<bool> ExistsAsync(Guid connectionId, string externalEventId, CancellationToken ct)
        => db.WebhookEvents.AnyAsync(
            x => x.ConnectionId == connectionId && x.ExternalEventId == externalEventId,
            ct);

    public async Task AddAsync(WebhookEvent webhookEvent, CancellationToken ct)
        => await db.WebhookEvents.AddAsync(webhookEvent, ct);

    public Task SaveAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
