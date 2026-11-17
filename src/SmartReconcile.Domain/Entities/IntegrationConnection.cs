using SmartReconcile.Domain.Enums;

namespace SmartReconcile.Domain.Entities;

public sealed class IntegrationConnection
{
    private IntegrationConnection() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public IntegrationProvider Provider { get; private set; }
    public string ExternalStoreId { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string ApiKeyHash { get; private set; } = string.Empty;
    public ConnectionStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? LastSeenAtUtc { get; private set; }

    public static IntegrationConnection Create(
        Guid tenantId,
        IntegrationProvider provider,
        string externalStoreId,
        string displayName,
        string apiKeyHash)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant id is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(externalStoreId)) throw new ArgumentException("External store id is required.", nameof(externalStoreId));
        if (string.IsNullOrWhiteSpace(apiKeyHash)) throw new ArgumentException("API key hash is required.", nameof(apiKeyHash));

        return new IntegrationConnection
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Provider = provider,
            ExternalStoreId = externalStoreId.Trim(),
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? externalStoreId.Trim() : displayName.Trim(),
            ApiKeyHash = apiKeyHash,
            Status = ConnectionStatus.Active,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public void Touch() => LastSeenAtUtc = DateTime.UtcNow;
    public void Revoke() => Status = ConnectionStatus.Revoked;
}
