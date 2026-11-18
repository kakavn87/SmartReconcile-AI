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
            var tenant = await tenants.GetByIdAsync(existingConnection.TenantId, cancellationToken)
                ?? throw new InvalidOperationException("The tenant for the Shopify connection no longer exists.");

            if (!tenant.IsActive)
                throw new UnauthorizedAccessException("The Shopify tenant is inactive.");

            existingConnection.Touch();
            await connections.SaveAsync(cancellationToken);
            return new ShopifyTenantResolution(tenant, existingConnection);
        }

        var tenantSlug = ToTenantSlug(shopDomain);
        var tenant = Tenant.Create(shopDomain, tenantSlug);

        // A shop domain is globally unique for Shopify, so an existing tenant with
        // the same slug means another connection already claimed this shop.
        if (await tenants.ExistsBySlugAsync(tenantSlug, cancellationToken))
        {
            var existingTenant = await FindTenantBySlugAsync(tenantSlug, cancellationToken);
            if (existingTenant is null)
                throw new InvalidOperationException("Shopify tenant slug exists but the tenant could not be resolved.");

            tenant = existingTenant;
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

    private async Task<Tenant?> FindTenantBySlugAsync(
        string slug,
        CancellationToken cancellationToken)
    {
        // The repository contract intentionally stays small. Resolve the existing
        // tenant through the integration connection that owns this Shopify domain.
        var connection = await connections.GetShopifyByDomainAsync(
            slug + ".myshopify.com",
            cancellationToken);

        return connection is null
            ? null
            : await tenants.GetByIdAsync(connection.TenantId, cancellationToken);
    }

    internal static string NormalizeShopDomain(string shopDomain)
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

    internal static string ToTenantSlug(string shopDomain)
    {
        var normalized = NormalizeShopDomain(shopDomain);
        return normalized[..^".myshopify.com".Length];
    }
}
