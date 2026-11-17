using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using SmartReconcile.Application.Abstractions;
using SmartReconcile.Domain.Entities;
using SmartReconcile.Domain.Enums;

namespace SmartReconcile.Infrastructure.Services;

public sealed class ShopifyClient(
    IHttpClientFactory httpClientFactory,
    ISecretProtector secretProtector) : IShopifyClient
{
    public async Task<IReadOnlyList<ShopifyOrder>> GetLatestOrdersAsync(
        IntegrationConnection connection,
        CancellationToken cancellationToken)
    {
        if (connection.Provider != IntegrationProvider.Shopify ||
            string.IsNullOrWhiteSpace(connection.ShopDomain) ||
            string.IsNullOrWhiteSpace(connection.EncryptedAccessToken))
            throw new InvalidOperationException("Keine gültige Shopify-Verbindung.");

        var token = secretProtector.Unprotect(connection.EncryptedAccessToken);
        using var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Shopify-Access-Token", token);

        const string query = """
        query LatestOrders {
          orders(first: 50, sortKey: CREATED_AT, reverse: true) {
            nodes {
              id
              name
              createdAt
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
          }
        }
        """;

        using var response = await client.PostAsJsonAsync(
            $"https://{connection.ShopDomain}/admin/api/2026-10/graphql.json",
            new { query },
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<GraphQlResponse>(
            cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Shopify lieferte keine Antwort.");

        if (payload.Errors?.Count > 0)
            throw new InvalidOperationException(payload.Errors[0].Message);

        return payload.Data?.Orders?.Nodes?.Select(MapOrder).ToList()
            ?? [];
    }

    private static ShopifyOrder MapOrder(OrderNode order)
    {
        var amount = decimal.Parse(order.TotalPriceSet?.ShopMoney?.Amount ?? "0", System.Globalization.CultureInfo.InvariantCulture);
        var transactions = order.Transactions?.Select(x => new ShopifyTransaction(
            x.Id,
            decimal.Parse(x.AmountSet?.ShopMoney?.Amount ?? "0", System.Globalization.CultureInfo.InvariantCulture),
            x.AmountSet?.ShopMoney?.CurrencyCode ?? "EUR",
            x.Status ?? "UNKNOWN",
            x.Kind ?? "UNKNOWN",
            0m)).ToList() ?? [];

        var financialStatus = order.DisplayFinancialStatus ?? "PENDING";
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
    private sealed record GraphQlOrders(List<OrderNode>? Nodes);
    private sealed record OrderNode(
        string Id,
        string? Name,
        DateTime CreatedAtUtc,
        string? DisplayFinancialStatus,
        MoneySet? TotalPriceSet,
        CustomerNode? Customer,
        List<TransactionNode>? Transactions);
    private sealed record TransactionNode(
        string Id,
        MoneySet? AmountSet,
        string? Status,
        string? Kind,
        DateTime? CreatedAtUtc);
    private sealed record MoneySet(Money? ShopMoney);
    private sealed record Money(string Amount, string CurrencyCode);
    private sealed record CustomerNode(string? DisplayName, string? Email);
}
