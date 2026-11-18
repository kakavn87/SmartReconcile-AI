using SmartReconcile.Domain.Entities;

namespace SmartReconcile.Application.Abstractions;

public interface IShopifyOAuthService
{
    string BuildInstallUrl(string shopDomain, string state);
    Task<ShopifyOAuthResult> ExchangeCodeAsync(string shopDomain, string code, CancellationToken cancellationToken);
    Task RegisterWebhooksAsync(string shopDomain, string accessToken, CancellationToken cancellationToken);
    bool VerifyCallbackHmac(IReadOnlyDictionary<string, string?> parameters);
    bool IsValidShopDomain(string shopDomain);
}

public interface IShopifyWebhookService
{
    Task<ShopifyWebhookResult> EnqueueAsync(
        string rawBody,
        string hmacHeader,
        string? shopDomain,
        string? topic,
        string? webhookId,
        CancellationToken cancellationToken);

    Task<int> ProcessPendingAsync(CancellationToken cancellationToken);
}

public interface IShopifyClient
{
    Task<IReadOnlyList<ShopifyOrder>> GetLatestOrdersAsync(
        IntegrationConnection connection,
        CancellationToken cancellationToken);
}

public sealed record ShopifyOAuthResult(string AccessToken, string Scope, string RefreshToken, int ExpiresInSeconds, int RefreshTokenExpiresInSeconds);

public sealed record ShopifyWebhookResult(
    Guid EventId,
    bool Duplicate,
    bool Verified,
    string Topic,
    string? ShopDomain);

public sealed record ShopifyOrder(
    string ExternalOrderId,
    string OrderNumber,
    string CustomerName,
    string? CustomerEmail,
    decimal TotalAmount,
    string Currency,
    string FinancialStatus,
    DateTime CreatedAtUtc,
    IReadOnlyList<ShopifyTransaction> Transactions);

public sealed record ShopifyTransaction(
    string ExternalTransactionId,
    decimal Amount,
    string Currency,
    string Status,
    string Kind,
    decimal FeeAmount,
    DateTime CreatedAtUtc);
