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
    public string? ApiKeyHash { get; private set; }
    public string? ShopDomain { get; private set; }
    public string ShopCurrency { get; private set; } = "EUR";
    public string? EncryptedAccessToken { get; private set; }
    public string? EncryptedRefreshToken { get; private set; }
    public DateTime? AccessTokenExpiresAtUtc { get; private set; }
    public DateTime? RefreshTokenExpiresAtUtc { get; private set; }
    public string Scopes { get; private set; } = string.Empty;
    public ConnectionStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? LastSeenAtUtc { get; private set; }
    public DateTime? LastSyncAtUtc { get; private set; }
    public DateTime? LastWebhookAtUtc { get; private set; }
    public string? InvoiceAccessTokenHash { get; private set; }
    public string? InvoiceAccessTokenLast4 { get; private set; }
    public DateTime? InvoiceAccessTokenUpdatedAtUtc { get; private set; }

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
            ApiKeyHash = apiKeyHash.Trim().ToLowerInvariant(),
            Status = ConnectionStatus.Active,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public static IntegrationConnection CreateShopify(
        Guid tenantId,
        string shopDomain,
        string displayName,
        string encryptedAccessToken,
        string scopes,
        string? encryptedRefreshToken = null,
        DateTime? accessTokenExpiresAtUtc = null,
        DateTime? refreshTokenExpiresAtUtc = null)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant id is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(shopDomain)) throw new ArgumentException("Shop domain is required.", nameof(shopDomain));
        if (string.IsNullOrWhiteSpace(encryptedAccessToken)) throw new ArgumentException("Encrypted access token is required.", nameof(encryptedAccessToken));

        var normalizedDomain = shopDomain.Trim().ToLowerInvariant();

        return new IntegrationConnection
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Provider = IntegrationProvider.Shopify,
            ExternalStoreId = normalizedDomain,
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? normalizedDomain : displayName.Trim(),
            ShopDomain = normalizedDomain,
            EncryptedAccessToken = encryptedAccessToken,
            EncryptedRefreshToken = encryptedRefreshToken,
            AccessTokenExpiresAtUtc = accessTokenExpiresAtUtc,
            RefreshTokenExpiresAtUtc = refreshTokenExpiresAtUtc,
            Scopes = scopes?.Trim() ?? string.Empty,
            Status = ConnectionStatus.Connected,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public void SetInvoiceAccessTokenHash(string hash, string last4)
    {
        InvoiceAccessTokenHash = hash;
        InvoiceAccessTokenLast4 = last4;
        InvoiceAccessTokenUpdatedAtUtc = DateTime.UtcNow;
    }

    public void RevokeInvoiceAccessToken()
    {
        InvoiceAccessTokenHash = null;
        InvoiceAccessTokenLast4 = null;
        InvoiceAccessTokenUpdatedAtUtc = DateTime.UtcNow;
    }

    public void Touch()
    {
        LastSeenAtUtc = DateTime.UtcNow;
    }

    public void MarkWebhookReceived()
    {
        LastWebhookAtUtc = DateTime.UtcNow;
        LastSeenAtUtc = LastWebhookAtUtc;
        Status = ConnectionStatus.Connected;
    }

    public void SetShopCurrency(string currency)
    {
        if (!string.IsNullOrWhiteSpace(currency))
            ShopCurrency = currency.Trim().ToUpperInvariant();
    }

    public void MarkSynced()
    {
        LastSyncAtUtc = DateTime.UtcNow;
        LastSeenAtUtc = LastSyncAtUtc;
        if (Status == ConnectionStatus.Warning)
            Status = ConnectionStatus.Connected;
    }

    public void SetWarning() => Status = ConnectionStatus.Warning;

    public void Disconnect() => Status = ConnectionStatus.Disconnected;

    public void UpdateShopifyCredentials(string encryptedAccessToken, string scopes, string? encryptedRefreshToken = null, DateTime? accessTokenExpiresAtUtc = null, DateTime? refreshTokenExpiresAtUtc = null)
    {
        if (Provider != IntegrationProvider.Shopify)
            throw new InvalidOperationException("Only Shopify connections can update Shopify credentials.");
        if (string.IsNullOrWhiteSpace(encryptedAccessToken))
            throw new ArgumentException("Encrypted access token is required.", nameof(encryptedAccessToken));

        EncryptedAccessToken = encryptedAccessToken;
        EncryptedRefreshToken = encryptedRefreshToken;
        AccessTokenExpiresAtUtc = accessTokenExpiresAtUtc;
        RefreshTokenExpiresAtUtc = refreshTokenExpiresAtUtc;
        Scopes = scopes?.Trim() ?? string.Empty;
        Status = ConnectionStatus.Connected;
    }


    public void Revoke()
    {
        Status = ConnectionStatus.Revoked;
        LastSeenAtUtc = null;
    }

    public void ClearShopifyCredentials()
    {
        if (Provider != IntegrationProvider.Shopify)
            return;

        EncryptedAccessToken = null;
        EncryptedRefreshToken = null;
        AccessTokenExpiresAtUtc = null;
        RefreshTokenExpiresAtUtc = null;
    }
}
