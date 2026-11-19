using SmartReconcile.Domain.Entities;
using SmartReconcile.Domain.Enums;
using SmartReconcile.Application.Abstractions;

namespace SmartReconcile.API.Authentication;

public interface IShopifyTenantResolutionService
{
    Task<ShopifyTenantResolution> ResolveAsync(
        ShopifySession session,
        string idToken,
        CancellationToken cancellationToken);
}

public sealed record ShopifyTenantResolution(
    Tenant Tenant,
    IntegrationConnection Connection);

public sealed class ShopifyTenantResolutionService(
    ITenantRepository tenants,
    IIntegrationConnectionRepository connections,
    IShopifyAuthenticationService authentication,
    ISecretProtector secretProtector) : IShopifyTenantResolutionService
{
    public async Task<ShopifyTenantResolution> ResolveAsync(
        ShopifySession session,
        string idToken,
        CancellationToken cancellationToken)
    {
        var shopDomain = NormalizeShopDomain(session.ShopDomain);
        var existingConnection = await connections.GetShopifyByDomainAsync(shopDomain, cancellationToken);

        if (existingConnection is not null)
        {
            var existingTenant = await tenants.GetByIdAsync(existingConnection.TenantId, cancellationToken)
                ?? throw new InvalidOperationException("The tenant for the Shopify connection no longer exists.");

            if (!existingTenant.IsActive)
                throw new UnauthorizedAccessException("The Shopify tenant is inactive.");

            // Embedded App sessions are the source of truth for the current shop.
            // Older connections created before token exchange/refresh support may
            // have no encrypted access token. Re-hydrate those credentials from
            // the current Shopify ID token instead of allowing a manual sync to
            // fail with "Keine gültige Shopify-Verbindung.".
            if (string.IsNullOrWhiteSpace(existingConnection.EncryptedAccessToken))
            {
                var token = await authentication.ExchangeIdTokenAsync(
                    shopDomain,
                    idToken,
                    cancellationToken);

                existingConnection.UpdateShopifyCredentials(
                    secretProtector.Protect(token.AccessToken),
                    token.Scope,
                    string.IsNullOrWhiteSpace(token.RefreshToken)
                        ? existingConnection.EncryptedRefreshToken
                        : secretProtector.Protect(token.RefreshToken),
                    DateTime.UtcNow.AddSeconds(token.ExpiresInSeconds),
                    token.RefreshTokenExpiresInSeconds > 0
                        ? DateTime.UtcNow.AddSeconds(token.RefreshTokenExpiresInSeconds)
                        : existingConnection.RefreshTokenExpiresAtUtc);
            }

            existingConnection.Touch();
            await connections.SaveAsync(cancellationToken);
            return new ShopifyTenantResolution(existingTenant, existingConnection);
        }

        var tenantSlug = ToTenantSlug(shopDomain);
        var tenant = Tenant.Create(shopDomain, tenantSlug);

        // A shop domain is globally unique for Shopify, so an existing tenant with
        // the same slug means another connection already claimed this shop.
        if (await tenants.ExistsBySlugAsync(tenantSlug, cancellationToken))
        {
            tenant = await tenants.GetBySlugAsync(tenantSlug, cancellationToken)
                ?? throw new InvalidOperationException("Shopify tenant slug exists but the tenant could not be resolved.");
        }
        else
        {
            await tenants.AddAsync(tenant, cancellationToken);
        }

        var token = await authentication.ExchangeIdTokenAsync(
            shopDomain,
            idToken,
            cancellationToken);

        var connection = IntegrationConnection.CreateShopify(
            tenant.Id,
            shopDomain,
            shopDomain,
            secretProtector.Protect(token.AccessToken),
            token.Scope,
            string.IsNullOrWhiteSpace(token.RefreshToken)
                ? null
                : secretProtector.Protect(token.RefreshToken),
            DateTime.UtcNow.AddSeconds(token.ExpiresInSeconds),
            token.RefreshTokenExpiresInSeconds > 0
                ? DateTime.UtcNow.AddSeconds(token.RefreshTokenExpiresInSeconds)
                : null);

        await connections.AddAsync(connection, cancellationToken);
        return new ShopifyTenantResolution(tenant, connection);
    }

    public static string NormalizeShopDomain(string shopDomain)
    {
        var normalized = shopDomain.Trim().ToLowerInvariant();

        if (normalized.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            normalized = normalized["https://".Length..];

        if (normalized.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            normalized = normalized["http://".Length..];

        normalized = normalized.TrimEnd('/');

        if (!normalized.EndsWith(".myshopify.com", StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("Invalid Shopify shop domain.");

        return normalized;
    }

    public static string ToTenantSlug(string shopDomain)
    {
        var normalized = NormalizeShopDomain(shopDomain);
        return normalized[..^".myshopify.com".Length];
    }
}
