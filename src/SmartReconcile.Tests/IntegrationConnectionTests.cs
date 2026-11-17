using SmartReconcile.Domain.Entities;
using SmartReconcile.Domain.Enums;
using System.Security.Cryptography;\nusing Microsoft.Extensions.Configuration;\nusing SmartReconcile.Infrastructure.Services;

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
        var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Security:EncryptionKey"] = key
            })
            .Build();

        var protector = new AesGcmSecretProtector(configuration);
        const string secret = "shpat_sensitive_access_token";

        var protectedValue = protector.Protect(secret);

        Assert.NotEqual(secret, protectedValue);
        Assert.Equal(secret, protector.Unprotect(protectedValue));
    }
}
