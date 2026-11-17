namespace SmartReconcile.Application.Abstractions;

public interface IShopifySyncService
{
    Task<ShopifySyncResult> SyncAsync(Guid connectionId, CancellationToken cancellationToken);
}

public sealed record ShopifySyncResult(
    Guid ConnectionId,
    int OrdersImported,
    int PaymentsImported,
    int SkippedOrders,
    DateTime CompletedAtUtc);
