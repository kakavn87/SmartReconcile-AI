namespace SmartReconcile.API.Authentication;

public sealed record ShopifySession(
    string ShopDomain,
    string UserId,
    string SessionId,
    string TokenId,
    DateTimeOffset ExpiresAtUtc,
    bool IsDemoMode);
