using SmartReconcile.Application.DTOs;

namespace SmartReconcile.Application.Abstractions;

public interface IIntegrationService
{
    Task<TenantDto> CreateTenantAsync(CreateTenantRequest request, CancellationToken cancellationToken);
    Task<IntegrationConnectionDto> CreateConnectionAsync(CreateIntegrationConnectionRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<IntegrationConnectionDto>> GetConnectionsAsync(Guid tenantId, CancellationToken cancellationToken);
    Task<WebhookAcceptedDto> ReceiveWebhookAsync(
        string provider,
        string apiKey,
        string externalEventId,
        string eventType,
        string payloadJson,
        CancellationToken cancellationToken);
}
