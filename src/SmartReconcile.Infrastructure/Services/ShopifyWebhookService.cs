using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SmartReconcile.API.Integrations;
using SmartReconcile.Application.Abstractions;
using SmartReconcile.Domain.Entities;
using SmartReconcile.Domain.Enums;
using SmartReconcile.Infrastructure.Persistence;

namespace SmartReconcile.Infrastructure.Services;

public sealed class ShopifyWebhookService(
    IOptions<ShopifyOptions> options,
    SmartReconcileDbContext db) : IShopifyWebhookService
{
    private readonly ShopifyOptions options = options.Value;

    public async Task<ShopifyWebhookResult> ProcessAsync(
        string rawBody,
        string hmacHeader,
        string? shopDomain,
        string? topic,
        string? webhookId,
        CancellationToken cancellationToken)
    {
        if (!VerifyHmac(rawBody, hmacHeader))
            throw new UnauthorizedAccessException("Ungültige Shopify-Webhook-Signatur.");

        if (string.IsNullOrWhiteSpace(webhookId))
            throw new ArgumentException("Shopify Webhook-ID fehlt.", nameof(webhookId));

        if (!string.IsNullOrWhiteSpace(shopDomain) && !IsShopDomain(shopDomain))
            throw new UnauthorizedAccessException("Ungültige Shopify-Shop-Domain.");

        var connection = await db.IntegrationConnections
            .FirstOrDefaultAsync(
                x => x.Provider == IntegrationProvider.Shopify &&
                     x.ShopDomain == shopDomain,
                cancellationToken);

        if (connection is null)
            throw new KeyNotFoundException("Shopify-Verbindung wurde nicht gefunden.");

        var duplicate = await db.WebhookEvents.AnyAsync(
            x => x.ConnectionId == connection.Id && x.ExternalEventId == webhookId,
            cancellationToken);

        if (duplicate)
            return new ShopifyWebhookResult(Guid.Empty, true, true, topic ?? "unknown", shopDomain);

        using var json = JsonDocument.Parse(rawBody);
        var eventType = topic ?? "unknown";

        var webhook = WebhookEvent.Create(
            connection.TenantId,
            connection.Id,
            webhookId,
            eventType,
            rawBody);

        connection.MarkWebhookReceived();
        db.WebhookEvents.Add(webhook);
        await db.SaveChangesAsync(cancellationToken);

        return new ShopifyWebhookResult(
            webhook.Id,
            false,
            true,
            eventType,
            shopDomain);
    }

    private bool VerifyHmac(string rawBody, string supplied)
    {
        if (string.IsNullOrWhiteSpace(supplied) || string.IsNullOrWhiteSpace(options.ClientSecret))
            return false;

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(options.ClientSecret));
        var digest = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(rawBody)));

        try
        {
            return CryptographicOperations.FixedTimeEquals(
                Convert.FromBase64String(digest),
                Convert.FromBase64String(supplied));
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static bool IsShopDomain(string value)
    {
        var shop = value.Trim().ToLowerInvariant();
        return shop.EndsWith(".myshopify.com", StringComparison.Ordinal)
            && shop.Count(c => c == '.') == 2
            && shop.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '.');
    }
}
