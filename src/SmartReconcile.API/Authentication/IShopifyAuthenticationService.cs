namespace SmartReconcile.API.Authentication;

public interface IShopifyAuthenticationService
{
    Task<ShopifySession> AuthenticateAsync(
        string idToken,
        CancellationToken cancellationToken);

    Task<ShopifyTokenExchangeResult> ExchangeIdTokenAsync(
        string shopDomain,
        string idToken,
        CancellationToken cancellationToken);
}

public sealed record ShopifyTokenExchangeResult(
    string AccessToken,
    string Scope,
    int ExpiresInSeconds,
    string RefreshToken,
    int RefreshTokenExpiresInSeconds);
