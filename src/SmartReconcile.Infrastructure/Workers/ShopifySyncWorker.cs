using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SmartReconcile.Application.Abstractions;
using SmartReconcile.Domain.Enums;
using SmartReconcile.Infrastructure.Persistence;

namespace SmartReconcile.Infrastructure.Workers;

public sealed class ShopifySyncWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<ShopifySyncWorker> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(15);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunOnce(stoppingToken);
                await timer.WaitForNextTickAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Shopify scheduled sync worker failed.");

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
    }

    private async Task RunOnce(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartReconcileDbContext>();
        var sync = scope.ServiceProvider.GetRequiredService<IShopifySyncService>();

        var connections = await db.IntegrationConnections
            .Where(x =>
                x.Provider == IntegrationProvider.Shopify &&
                x.Status == ConnectionStatus.Connected &&
                x.EncryptedAccessToken != null &&
                (x.LastSyncAtUtc == null || x.LastSyncAtUtc <= DateTime.UtcNow.AddMinutes(-15)))
            .OrderBy(x => x.LastSyncAtUtc)
            .ToListAsync(ct);

        if (connections.Count == 0)
            return;

        logger.LogInformation(
            "Starting scheduled Shopify sync for {ConnectionCount} connection(s).",
            connections.Count);

        foreach (var connection in connections)
        {
            try
            {
                var result = await sync.SyncAsync(connection.Id, ct);
                logger.LogInformation(
                    "Scheduled Shopify sync completed for {ConnectionId}: imported={Imported}, updated={Updated}, payments={Payments}.",
                    connection.Id,
                    result.OrdersImported,
                    result.SkippedOrders,
                    result.PaymentsImported);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Scheduled Shopify sync failed for connection {ConnectionId} and tenant {TenantId}.",
                    connection.Id,
                    connection.TenantId);
            }
        }
    }
}
