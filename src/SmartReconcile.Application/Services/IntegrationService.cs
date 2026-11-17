using System.Security.Cryptography;
using System.Text;
using SmartReconcile.Application.Abstractions;
using SmartReconcile.Application.DTOs;
using SmartReconcile.Domain.Entities;
using SmartReconcile.Domain.Enums;

namespace SmartReconcile.Application.Services;

public sealed class IntegrationService(
    ITenantRepository tenants,
    IIntegrationConnectionRepository connections,
    IWebhookEventRepository webhookEvents) : IIntegrationService
{
    public async Task<TenantDto> CreateTenantAsync(CreateTenantRequest request, CancellationToken cancellationToken)
    {
        var slug = request.Slug.Trim().ToLowerInvariant();

        if (await tenants.ExistsBySlugAsync(slug, cancellationToken))
            throw new InvalidOperationException($"Tenant slug '{slug}' already exists.");

        var tenant = Tenant.Create(request.Name, slug);
        await tenants.AddAsync(tenant, cancellationToken);
        return MapTenant(tenant);
    }

    public async Task<IntegrationConnectionDto> CreateConnectionAsync(
        CreateIntegrationConnectionRequest request,
        CancellationToken cancellationToken)
    {
        if (await tenants.GetByIdAsync(request.TenantId, cancellationToken) is null)
            throw new KeyNotFoundException("Tenant not found.");

        var plainApiKey = GenerateApiKey();
        var connection = IntegrationConnection.Create(
            request.TenantId,
            request.Provider,
            request.ExternalStoreId,
            request.DisplayName,
            HashApiKey(plainApiKey));

        await connections.AddAsync(connection, cancellationToken);

        return MapConnection(connection, plainApiKey);
    }

    public async Task<IReadOnlyList<IntegrationConnectionDto>> GetConnectionsAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var items = await connections.GetForTenantAsync(tenantId, cancellationToken);
        return items.Select(x => MapConnection(x, null)).ToList();
    }

    public async Task<WebhookAcceptedDto> ReceiveWebhookAsync(
        string provider,
        string apiKey,
        string externalEventId,
        string eventType,
        string payloadJson,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<IntegrationProvider>(provider, true, out var parsedProvider))
            throw new ArgumentException($"Unsupported integration provider '{provider}'.");

        var connection = await connections.ResolveActiveByApiKeyHashAsync(HashApiKey(apiKey), cancellationToken)
            ?? throw new UnauthorizedAccessException("Invalid integration API key.");

        if (connection.Provider != parsedProvider)
            throw new UnauthorizedAccessException("The API key is not valid for this provider.");

        if (await webhookEvents.ExistsAsync(connection.Id, externalEventId, cancellationToken))
        {
            return new WebhookAcceptedDto(
                Guid.Empty,
                true,
                parsedProvider.ToString(),
                eventType,
                DateTime.UtcNow);
        }

        var webhookEvent = WebhookEvent.Create(
            connection.TenantId,
            connection.Id,
            externalEventId,
            eventType,
            payloadJson);

        connection.MarkWebhookReceived();
        await webhookEvents.AddAsync(webhookEvent, cancellationToken);
        await webhookEvents.SaveAsync(cancellationToken);

        return new WebhookAcceptedDto(
            webhookEvent.Id,
            false,
            parsedProvider.ToString(),
            eventType,
            webhookEvent.ReceivedAtUtc);
    }

    private static string GenerateApiKey()
        => "sr_live_" + Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();

    private static string HashApiKey(string value)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private static TenantDto MapTenant(Tenant x)
        => new(x.Id, x.Name, x.Slug, x.IsActive, x.CreatedAtUtc);

    private static IntegrationConnectionDto MapConnection(IntegrationConnection x, string? apiKey)
        => new(x.Id, x.TenantId, x.Provider, x.ExternalStoreId, x.DisplayName, x.Status, x.CreatedAtUtc, x.LastSeenAtUtc, x.ShopDomain, x.Scopes, x.LastSyncAtUtc, x.LastWebhookAtUtc, apiKey);
}
