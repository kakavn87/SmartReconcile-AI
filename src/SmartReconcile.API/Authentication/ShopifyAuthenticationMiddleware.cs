using Microsoft.Extensions.Options;

namespace SmartReconcile.API.Authentication;

public sealed class ShopifyAuthenticationMiddleware(
    RequestDelegate next,
    IOptions<ShopifyAuthenticationOptions> options)
{
    public async Task InvokeAsync(
        HttpContext context,
        IShopifyAuthenticationService authentication)
    {
        var authorization = context.Request.Headers.Authorization.FirstOrDefault();

        if (string.IsNullOrWhiteSpace(authorization))
        {
            // Demo Mode is intentionally limited to requests without an Authorization
            // header. Phase 2 will mark merchant/business API routes as requiring
            // Shopify authentication and resolve their Tenant from the verified token.
            if (options.Value.DemoModeEnabled)
                context.Items[ShopifyAuthenticationContext.DemoModeItemKey] = true;

            await next(context);
            return;
        }

        if (!authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            await WriteUnauthorizedAsync(context, "Invalid Authorization header.");
            return;
        }

        var idToken = authorization["Bearer ".Length..].Trim();

        try
        {
            var session = await authentication.AuthenticateAsync(idToken, context.RequestAborted);
            context.Items[ShopifyAuthenticationContext.SessionItemKey] = session;
            await next(context);
        }
        catch (UnauthorizedAccessException ex)
        {
            context.Response.Headers["X-Shopify-Retry-Invalid-Session-Request"] = "1";
            await WriteUnauthorizedAsync(context, ex.Message);
        }
    }

    private static async Task WriteUnauthorizedAsync(HttpContext context, string message)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { message });
    }
}

public static class ShopifyAuthenticationContext
{
    public const string SessionItemKey = "SmartReconcile.ShopifySession";
    public const string DemoModeItemKey = "SmartReconcile.DemoMode";

    public static ShopifySession? GetSession(HttpContext context)
        => context.Items.TryGetValue(SessionItemKey, out var value)
            ? value as ShopifySession
            : null;

    public static bool IsDemoMode(HttpContext context)
        => context.Items.ContainsKey(DemoModeItemKey);
}
