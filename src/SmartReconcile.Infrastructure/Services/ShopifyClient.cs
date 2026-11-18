using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SmartReconcile.Infrastructure.Persistence;
using SmartReconcile.Application.Abstractions;
using SmartReconcile.Domain.Entities;
using SmartReconcile.Domain.Enums;

namespace SmartReconcile.Infrastructure.Services;

public sealed class ShopifyClient(
    IHttpClientFactory httpClientFactory,
    ISecretProtector secretProtector,
    IOptions<ShopifyOptions> options,
    SmartReconcileDbContext db) : IShopifyClient
{
    public async Task<IReadOnlyList<ShopifyOrder>> GetLatestOrdersAsync(
        IntegrationConnection connection,
        CancellationToken cancellationToken)
    {
        if (connection.Provider != IntegrationProvider.Shopify ||
            string.IsNullOrWhiteSpace(connection.ShopDomain) ||
            string.IsNullOrWhiteSpace(connection.EncryptedAccessToken))
            throw new InvalidOperationException("Keine gültige Shopify-Verbindung.");

        var token = await GetValidAccessTokenAsync(connection, cancellationToken);
        using var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Shopify-Access-Token", token);

        const string query = """
        query LatestOrders($after: String) {
          orders(first: 50, after: $after, sortKey: CREATED_AT, reverse: true) {
            nodes {
              id
              name
              createdAt
              cancelledAt
              displayFinancialStatus
              totalPriceSet { shopMoney { amount currencyCode } }
              customer { displayName email }
              transactions(first: 20) {
                id
                amountSet { shopMoney { amount currencyCode } }
                status
                kind
                createdAt
              }
            }
            pageInfo { hasNextPage endCursor }
          }
        }
        """;

        var orders = new List<ShopifyOrder>();
        string? cursor = null;

        do
        {
            using var response = await client.PostAsJsonAsync(
                $"https://{connection.ShopDomain}/admin/api/2026-10/graphql.json",
                new { query, variables = new { after = cursor } },
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var payload = await response.Content.ReadFromJsonAsync<GraphQlResponse>(
                cancellationToken: cancellationToken)
                ?? throw new InvalidOperationException("Shopify lieferte keine Antwort.");

            if (payload.Errors?.Count > 0)
                throw new InvalidOperationException(payload.Errors[0].Message);

            var page = payload.Data?.Orders
                ?? throw new InvalidOperationException("Shopify lieferte keine Bestellungen.");

            orders.AddRange(page.Nodes?.Select(MapOrder) ?? []);

            if (!page.PageInfo.HasNextPage)
                break;

            if (string.IsNullOrWhiteSpace(page.PageInfo.EndCursor))
                throw new InvalidOperationException("Shopify lieferte keinen Cursor für die nächste Bestellungsseite.");

            cursor = page.PageInfo.EndCursor;
        }
        while (!cancellationToken.IsCancellationRequested);

        return orders;
    }

    private async Task<string> GetValidAccessTokenAsync(IntegrationConnection connection, CancellationToken cancellationToken)
    {
        if (connection.AccessTokenExpiresAtUtc is null ||
            connection.AccessTokenExpiresAtUtc > DateTime.UtcNow.AddMinutes(5) ||
            string.IsNullOrWhiteSpace(connection.EncryptedRefreshToken))
            return secretProtector.Unprotect(connection.EncryptedAccessToken!);

        var refreshToken = secretProtector.Unprotect(connection.EncryptedRefreshToken);
        using var client = httpClientFactory.CreateClient();
        using var response = await client.PostAsJsonAsync(
            $"https://{connection.ShopDomain}/admin/oauth/access_token",
            new
            {
                client_id = options.Value.ClientId,
                client_secret = options.Value.ClientSecret,
                grant_type = "refresh_token",
                refresh_token = refreshToken
            },
            cancellationToken);

        response.EnsureSuccessStatusCode();
        var token = await response.Content.ReadFromJsonAsync<RefreshTokenResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Shopify lieferte keinen erneuerten Access Token.");

        if (string.IsNullOrWhiteSpace(token.AccessToken))
            throw new InvalidOperationException("Shopify lieferte keinen gültigen Access Token.");

        connection.UpdateShopifyCredentials(
            secretProtector.Protect(token.AccessToken),
            connection.Scopes,
            string.IsNullOrWhiteSpace(token.RefreshToken) ? connection.EncryptedRefreshToken : secretProtector.Protect(token.RefreshToken),
            DateTime.UtcNow.AddSeconds(token.ExpiresInSeconds),
            token.RefreshTokenExpiresInSeconds > 0 ? DateTime.UtcNow.AddSeconds(token.RefreshTokenExpiresInSeconds) : connection.RefreshTokenExpiresAtUtc);

        await db.SaveChangesAsync(cancellationToken);
        return token.AccessToken;
    }

    private sealed record RefreshTokenResponse(
        [property: System.Text.Json.Serialization.JsonPropertyName("access_token")] string? AccessToken,
        [property: System.Text.Json.Serialization.JsonPropertyName("refresh_token")] string? RefreshToken,
        [property: System.Text.Json.Serialization.JsonPropertyName("expires_in")] int ExpiresInSeconds,
        [property: System.Text.Json.Serialization.JsonPropertyName("refresh_token_expires_in")] int RefreshTokenExpiresInSeconds);

    private static ShopifyOrder MapOrder(OrderNode order)
    {
        var amount = ParseDecimal(order.TotalPriceSet?.ShopMoney?.Amount);
        var transactions = order.Transactions?.Select(x => new ShopifyTransaction(
            x.Id,
            ParseDecimal(x.AmountSet?.ShopMoney?.Amount),
            x.AmountSet?.ShopMoney?.CurrencyCode ?? "EUR",
            x.Status ?? "UNKNOWN",
            x.Kind ?? "UNKNOWN",
            0m,
            x.CreatedAtUtc ?? DateTime.UtcNow)).ToList() ?? [];

        var financialStatus = order.CancelledAtUtc is not null
            ? "CANCELLED"
            : order.DisplayFinancialStatus ?? "PENDING";
        return new ShopifyOrder(
            order.Id,
            order.Name ?? order.Id,
            order.Customer?.DisplayName ?? "Shopify Kunde",
            order.Customer?.Email,
            amount,
            order.TotalPriceSet?.ShopMoney?.CurrencyCode ?? "EUR",
            financialStatus,
            order.CreatedAtUtc,
            transactions);
    }

    private sealed record GraphQlResponse(GraphQlData? Data, List<GraphQlError>? Errors);
    private sealed record GraphQlError(string Message);
    private sealed record GraphQlData(GraphQlOrders? Orders);
    private sealed record GraphQlOrders(List<OrderNode>? Nodes, PageInfo PageInfo);
    private sealed record OrderNode(
        string Id,
        [property: System.Text.Json.Serialization.JsonPropertyName("name")] string? Name,
        [property: System.Text.Json.Serialization.JsonPropertyName("createdAt")] DateTime CreatedAtUtc,
        [property: System.Text.Json.Serialization.JsonPropertyName("cancelledAt")] DateTime? CancelledAtUtc,
        [property: System.Text.Json.Serialization.JsonPropertyName("displayFinancialStatus")] string? DisplayFinancialStatus,
        [property: System.Text.Json.Serialization.JsonPropertyName("totalPriceSet")] MoneySet? TotalPriceSet,
        [property: System.Text.Json.Serialization.JsonPropertyName("customer")] CustomerNode? Customer,
        [property: System.Text.Json.Serialization.JsonPropertyName("transactions")] List<TransactionNode>? Transactions);
    private sealed record TransactionNode(
        [property: System.Text.Json.Serialization.JsonPropertyName("id")] string Id,
        [property: System.Text.Json.Serialization.JsonPropertyName("amountSet")] MoneySet? AmountSet,
        [property: System.Text.Json.Serialization.JsonPropertyName("status")] string? Status,
        [property: System.Text.Json.Serialization.JsonPropertyName("kind")] string? Kind,
        [property: System.Text.Json.Serialization.JsonPropertyName("createdAt")] DateTime? CreatedAtUtc);
    private sealed record MoneySet([property: System.Text.Json.Serialization.JsonPropertyName("shopMoney")] Money? ShopMoney);
    private sealed record Money(
        [property: System.Text.Json.Serialization.JsonPropertyName("amount")] string Amount,
        [property: System.Text.Json.Serialization.JsonPropertyName("currencyCode")] string CurrencyCode);
    private sealed record PageInfo(
        [property: System.Text.Json.Serialization.JsonPropertyName("hasNextPage")] bool HasNextPage,
        [property: System.Text.Json.Serialization.JsonPropertyName("endCursor")] string? EndCursor);

    private static decimal ParseDecimal(string? value)
        => decimal.TryParse(
            value,
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture,
            out var parsed)
            ? parsed
            : 0m;
    private sealed record CustomerNode(string? DisplayName, string? Email);
}
