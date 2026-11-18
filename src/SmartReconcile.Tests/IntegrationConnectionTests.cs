using Xunit;
using SmartReconcile.Domain.Entities;
using SmartReconcile.Domain.Enums;
using System.Security.Cryptography;
using SmartReconcile.Infrastructure.Services;

namespace SmartReconcile.Tests;

public sealed class IntegrationConnectionTests
{
    [Fact]
    public void CreateShopify_StoresNormalizedStoreMetadata()
    {
        var tenantId = Guid.NewGuid();
        var connection = IntegrationConnection.CreateShopify(
            tenantId,
            " My-Shop.myshopify.com ",
            " My Shop ",
            "encrypted-token",
            "read_orders,read_shopify_payments_payouts");

        Assert.Equal(tenantId, connection.TenantId);
        Assert.Equal(IntegrationProvider.Shopify, connection.Provider);
        Assert.Equal("my-shop.myshopify.com", connection.ShopDomain);
        Assert.Equal("my-shop.myshopify.com", connection.ExternalStoreId);
        Assert.Equal("My Shop", connection.DisplayName);
        Assert.Equal("encrypted-token", connection.EncryptedAccessToken);
        Assert.Equal("read_orders,read_shopify_payments_payouts", connection.Scopes);
        Assert.Equal(ConnectionStatus.Connected, connection.Status);
    }

    [Fact]
    public void MarkWebhookReceived_UpdatesWebhookAndSeenTimestamps()
    {
        var connection = IntegrationConnection.CreateShopify(
            Guid.NewGuid(),
            "demo.myshopify.com",
            "Demo",
            "encrypted-token",
            "read_orders");

        connection.MarkWebhookReceived();

        Assert.NotNull(connection.LastWebhookAtUtc);
        Assert.Equal(connection.LastWebhookAtUtc, connection.LastSeenAtUtc);
        Assert.Equal(ConnectionStatus.Connected, connection.Status);
    }
}

public sealed class AesGcmSecretProtectorTests
{
    [Fact]
    public void ProtectAndUnprotect_RoundTripsWithoutStoringPlaintext()
    {
        var protector = new AesGcmSecretProtector(RandomNumberGenerator.GetBytes(32));
        const string secret = "shpat_sensitive_access_token";

        var protectedValue = protector.Protect(secret);

        Assert.NotEqual(secret, protectedValue);
        Assert.Equal(secret, protector.Unprotect(protectedValue));
    }
}
