using SmartReconcile.Domain.Enums;

namespace SmartReconcile.Application.DTOs;

public sealed record CreateTenantRequest(string Name, string Slug);

public sealed record TenantDto(
    Guid Id,
    string Name,
    string Slug,
    bool IsActive,
    DateTime CreatedAtUtc);

public sealed record CreateIntegrationConnectionRequest(
    Guid TenantId,
    IntegrationProvider Provider,
    string ExternalStoreId,
    string DisplayName);

public sealed record IntegrationConnectionDto(
    Guid Id,
    Guid TenantId,
    IntegrationProvider Provider,
    string ExternalStoreId,
    string DisplayName,
    ConnectionStatus Status,
    DateTime CreatedAtUtc,
    DateTime? LastSeenAtUtc,
    string? ApiKey);

public sealed record WebhookAcceptedDto(
    Guid EventId,
    bool Duplicate,
    string Provider,
    string EventType,
    DateTime ReceivedAtUtc);
