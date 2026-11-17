using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SmartReconcile.API.Integrations;
using SmartReconcile.Application.Abstractions;

namespace SmartReconcile.Infrastructure.Services;

public sealed class ShopifyOAuthService(
    IOptions<ShopifyOptions> options,
    IHttpClientFactory httpClientFactory) : IShopifyOAuthService
{
    private readonly ShopifyOptions options = options.Value;

    public string BuildInstallUrl(string shopDomain, string state)
    {
        if (!IsValidShopDomain(shopDomain))
            throw new ArgumentException("Invalid Shopify shop domain.", nameof(shopDomain));

        var redirectUri = $"{options.AppUrl.TrimEnd('/')}/api/integrations/shopify/callback";
        var query = new Dictionary<string, string>
        {
            ["client_id"] = options.ClientId,
            ["scope"] = options.Scopes,
            ["redirect_uri"] = redirectUri,
            ["state"] = state
        };

        return $"https://{shopDomain}/admin/oauth/authorize?{string.Join("&", query.Select(x => $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value)}"))}";
    }

    public async Task<ShopifyOAuthResult> ExchangeCodeAsync(string shopDomain, string code, CancellationToken cancellationToken)
    {
        if (!IsValidShopDomain(shopDomain))
            throw new ArgumentException("Invalid Shopify shop domain.", nameof(shopDomain));

        using var client = httpClientFactory.CreateClient();
        using var response = await client.PostAsJsonAsync(
            $"https://{shopDomain}/admin/oauth/access_token",
            new
            {
                client_id = options.ClientId,
                client_secret = options.ClientSecret,
                code,
                expiring = 1
            },
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<ShopifyTokenResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Shopify returned an empty token response.");

        if (string.IsNullOrWhiteSpace(payload.AccessToken))
            throw new InvalidOperationException("Shopify did not return an access token.");

        return new ShopifyOAuthResult(payload.AccessToken, payload.Scope ?? options.Scopes, payload.RefreshToken ?? string.Empty, payload.ExpiresInSeconds, payload.RefreshTokenExpiresInSeconds);
    }

    public async Task RegisterWebhooksAsync(string shopDomain, string accessToken, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Shopify-Access-Token", accessToken);

        const string mutation = """
        mutation CreateWebhook($topic: WebhookSubscriptionTopic!, $uri: URI!) {
          webhookSubscriptionCreate(topic: $topic, webhookSubscription: {callbackUrl: $uri, format: JSON}) {
            userErrors { field message }
          }
        }
        """;

        var topics = new[] { "ORDERS_CREATE", "ORDERS_UPDATED", "ORDERS_PAID", "REFUNDS_CREATE" };
        var callbackUrl = $"{options.AppUrl.TrimEnd('/')}/api/webhooks/shopify";

        foreach (var topic in topics)
        {
            using var response = await client.PostAsJsonAsync(
                $"https://{shopDomain}/admin/api/2026-10/graphql.json",
                new { query = mutation, variables = new { topic, uri = callbackUrl } },
                cancellationToken);

            response.EnsureSuccessStatusCode();
        }
    }

    public bool VerifyCallbackHmac(IReadOnlyDictionary<string, string?> parameters)
    {
        if (!parameters.TryGetValue("hmac", out var supplied) || string.IsNullOrWhiteSpace(supplied))
            return false;

        var message = string.Join("&",
            parameters
                .Where(x => !string.Equals(x.Key, "hmac", StringComparison.OrdinalIgnoreCase))
                .Where(x => x.Value is not null)
                .OrderBy(x => x.Key, StringComparer.Ordinal)
                .Select(x => $"{x.Key}={x.Value}"));

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(options.ClientSecret));
        var digest = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(message))).ToLowerInvariant();

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(digest),
            Encoding.UTF8.GetBytes(supplied.ToLowerInvariant()));
    }

    public bool IsValidShopDomain(string shopDomain)
    {
        if (string.IsNullOrWhiteSpace(shopDomain))
            return false;

        var value = shopDomain.Trim().ToLowerInvariant();
        return value.Length <= 255
            && value.EndsWith(".myshopify.com", StringComparison.Ordinal)
            && value.Count(c => c == '.') == 2
            && value[..^".myshopify.com".Length].Length >= 1
            && value.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '.');
    }

    private sealed record ShopifyTokenResponse(
        [property: System.Text.Json.Serialization.JsonPropertyName("access_token")] string? AccessToken,
        [property: System.Text.Json.Serialization.JsonPropertyName("scope")] string? Scope,
        [property: System.Text.Json.Serialization.JsonPropertyName("refresh_token")] string? RefreshToken,
        [property: System.Text.Json.Serialization.JsonPropertyName("expires_in")] int ExpiresInSeconds,
        [property: System.Text.Json.Serialization.JsonPropertyName("refresh_token_expires_in")] int RefreshTokenExpiresInSeconds);
}
