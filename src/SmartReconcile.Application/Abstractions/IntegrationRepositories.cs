using SmartReconcile.Domain.Entities;
using SmartReconcile.Domain.Enums;

namespace SmartReconcile.Application.Abstractions;

public interface ITenantRepository
{
    Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> ExistsBySlugAsync(string slug, CancellationToken cancellationToken);
    Task AddAsync(Tenant tenant, CancellationToken cancellationToken);
}

public interface IIntegrationConnectionRepository
{
    Task<IntegrationConnection?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IntegrationConnection?> ResolveActiveByApiKeyHashAsync(string apiKeyHash, CancellationToken cancellationToken);
    Task<List<IntegrationConnection>> GetForTenantAsync(Guid tenantId, CancellationToken cancellationToken);
    Task AddAsync(IntegrationConnection connection, CancellationToken cancellationToken);
}

public interface IWebhookEventRepository
{
    Task<bool> ExistsAsync(Guid connectionId, string externalEventId, CancellationToken cancellationToken);
    Task AddAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken);
    Task SaveAsync(CancellationToken cancellationToken);
}
