using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SmartReconcile.Application.Abstractions;

namespace SmartReconcile.API.Authentication;

public sealed class ShopifyAuthenticationService(
    IHttpClientFactory httpClientFactory,
    IOptions<ShopifyOptions> shopifyOptions) : IShopifyAuthenticationService
{
    public Task<ShopifySession> AuthenticateAsync(
        string idToken,
        CancellationToken cancellationToken)
    {
        var principal = ValidateIdToken(idToken);
        var shopDomain = GetAndValidateShopDomain(principal);
        var userId = principal.FindFirstValue("sub") ?? string.Empty;
        var sessionId = principal.FindFirstValue("sid") ?? string.Empty;
        var tokenId = principal.FindFirstValue("jti") ?? string.Empty;
        var expiresAt = GetUnixTimeClaim(principal, "exp");

        return Task.FromResult(new ShopifySession(
            shopDomain,
            userId,
            sessionId,
            tokenId,
            expiresAt,
            false));
    }

    public async Task<ShopifyTokenExchangeResult> ExchangeIdTokenAsync(
        string shopDomain,
        string idToken,
        CancellationToken cancellationToken)
    {
        var normalizedShop = shopDomain.Trim().ToLowerInvariant();

        using var client = httpClientFactory.CreateClient();
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = shopifyOptions.Value.ClientId,
            ["client_secret"] = shopifyOptions.Value.ClientSecret,
            ["grant_type"] = "urn:ietf:params:oauth:grant-type:token-exchange",
            ["requested_token_type"] = "urn:shopify:params:oauth:token-type:offline-access-token",
            ["subject_token"] = idToken,
            ["subject_token_type"] = "urn:shopify:params:oauth:token-type:id_token",
            ["expiring"] = "1"
        });

        using var response = await client.PostAsync(
            $"https://{normalizedShop}/admin/oauth/access_token",
            content,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new UnauthorizedAccessException(
                $"Shopify token exchange failed with {(int)response.StatusCode}: {body}");
        }

        var payload = await response.Content.ReadFromJsonAsync<ShopifyTokenExchangeResponse>(
            cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Shopify returned an empty token exchange response.");

        if (string.IsNullOrWhiteSpace(payload.AccessToken))
            throw new UnauthorizedAccessException("Shopify did not return an access token.");

        return new ShopifyTokenExchangeResult(
            payload.AccessToken,
            payload.Scope ?? string.Empty,
            payload.ExpiresIn,
            payload.RefreshToken ?? string.Empty,
            payload.RefreshTokenExpiresIn);
    }

    private ClaimsPrincipal ValidateIdToken(string idToken)
    {
        if (string.IsNullOrWhiteSpace(idToken))
            throw new UnauthorizedAccessException("Shopify ID token is required.");

        var parameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(shopifyOptions.Value.ClientSecret)),
            ValidateIssuer = true,
            IssuerValidator = ValidateIssuer,
            ValidateAudience = true,
            ValidAudience = shopifyOptions.Value.ClientId,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = "sub"
        };

        var handler = new JwtSecurityTokenHandler();
        try
        {
            var principal = handler.ValidateToken(idToken, parameters, out _);
            ValidateIssuerMatchesDestination(principal);
            return principal;
        }
        catch (SecurityTokenException ex)
        {
            throw new UnauthorizedAccessException("Invalid Shopify ID token.", ex);
        }
        catch (ArgumentException ex)
        {
            throw new UnauthorizedAccessException("Invalid Shopify ID token.", ex);
        }
    }

    private static string ValidateIssuer(
        string issuer,
        SecurityToken securityToken,
        TokenValidationParameters validationParameters)
    {
        if (!Uri.TryCreate(issuer, UriKind.Absolute, out var uri) ||
            !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(uri.AbsolutePath.TrimEnd('/'), "/admin", StringComparison.OrdinalIgnoreCase) ||
            !uri.Host.EndsWith(".myshopify.com", StringComparison.OrdinalIgnoreCase))
        {
            throw new SecurityTokenInvalidIssuerException("Invalid Shopify ID token issuer.");
        }

        return issuer;
    }

    private static void ValidateIssuerMatchesDestination(ClaimsPrincipal principal)
    {
        var issuer = principal.FindFirstValue("iss");
        var destination = principal.FindFirstValue("dest");

        if (!Uri.TryCreate(issuer, UriKind.Absolute, out var issuerUri) ||
            !Uri.TryCreate(destination, UriKind.Absolute, out var destinationUri) ||
            !string.Equals(issuerUri.Host, destinationUri.Host, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Shopify ID token issuer and destination do not match.");
        }
    }

    private static string GetAndValidateShopDomain(ClaimsPrincipal principal)
    {
        var destination = principal.FindFirstValue("dest");
        if (!Uri.TryCreate(destination, UriKind.Absolute, out var uri) ||
            !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
            !uri.Host.EndsWith(".myshopify.com", StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Shopify ID token contains an invalid shop domain.");
        }

        return uri.Host.ToLowerInvariant();
    }

    private static DateTimeOffset GetUnixTimeClaim(ClaimsPrincipal principal, string claimType)
    {
        var value = principal.FindFirstValue(claimType);
        if (!long.TryParse(value, out var unixSeconds))
            throw new UnauthorizedAccessException($"Shopify ID token is missing a valid '{claimType}' claim.");

        return DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
    }

    private sealed record ShopifyTokenExchangeResponse(
        [property: System.Text.Json.Serialization.JsonPropertyName("access_token")] string? AccessToken,
        [property: System.Text.Json.Serialization.JsonPropertyName("scope")] string? Scope,
        [property: System.Text.Json.Serialization.JsonPropertyName("expires_in")] int ExpiresIn,
        [property: System.Text.Json.Serialization.JsonPropertyName("refresh_token")] string? RefreshToken,
        [property: System.Text.Json.Serialization.JsonPropertyName("refresh_token_expires_in")] int RefreshTokenExpiresIn);
}
