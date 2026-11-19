using SmartReconcile.Domain.Entities;

namespace SmartReconcile.API.Authentication;

public sealed class ShopifyAuthenticationMiddleware(
    RequestDelegate next)
{
    public async Task InvokeAsync(
        HttpContext context,
        IShopifyAuthenticationService authentication,
        IShopifyTenantResolutionService tenantResolution)
    {
        if (IsPublicRoute(context.Request.Path))
        {
            await next(context);
            return;
        }

        var authorization = context.Request.Headers.Authorization.FirstOrDefault();

        if (string.IsNullOrWhiteSpace(authorization))
        {
            await WriteUnauthorizedAsync(context, "Shopify authentication is required.");
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
            var resolution = await tenantResolution.ResolveAsync(session, idToken, context.RequestAborted);
            context.Items[ShopifyAuthenticationContext.SessionItemKey] = session;
            context.Items[ShopifyAuthenticationContext.TenantItemKey] = resolution.Tenant;
            context.Items[ShopifyAuthenticationContext.ConnectionItemKey] = resolution.Connection;
            await next(context);
        }
        catch (UnauthorizedAccessException ex)
        {
            context.Response.Headers["X-Shopify-Retry-Invalid-Session-Request"] = "1";
            await WriteUnauthorizedAsync(context, ex.Message);
        }
    }

    private static bool IsPublicRoute(PathString path)
    {
        return path.StartsWithSegments("/health")
            || path.StartsWithSegments("/swagger")
            || path.Equals("/api/auth/shopify/config")
            || path.StartsWithSegments("/api/integrations/shopify/install")
            || path.StartsWithSegments("/api/integrations/shopify/callback")
            || path.StartsWithSegments("/api/webhooks/shopify")
            || path.StartsWithSegments("/public/invoices/shopify");
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
    public const string TenantItemKey = "SmartReconcile.ShopifyTenant";
    public const string ConnectionItemKey = "SmartReconcile.ShopifyConnection";

    public static ShopifySession? GetSession(HttpContext context)
        => context.Items.TryGetValue(SessionItemKey, out var value)
            ? value as ShopifySession
            : null;

    public static Tenant? GetTenant(HttpContext context)
        => context.Items.TryGetValue(TenantItemKey, out var value) ? value as Tenant : null;

    public static IntegrationConnection? GetConnection(HttpContext context)
        => context.Items.TryGetValue(ConnectionItemKey, out var value) ? value as IntegrationConnection : null;
}
