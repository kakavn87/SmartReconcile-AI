namespace SmartReconcile.API.Integrations;

public sealed class ShopifyOptions
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string Scopes { get; set; } = "read_orders,read_shopify_payments_payouts";
    public string AppUrl { get; set; } = string.Empty;
}
