using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SmartReconcile.Application.Abstractions;

namespace SmartReconcile.Infrastructure.Workers;

public sealed class ReconciliationWorker(IServiceScopeFactory scopeFactory, ILogger<ReconciliationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(15));
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunOnce(stoppingToken);
                await timer.WaitForNextTickAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                logger.LogError(ex, "Reconciliation background job failed.");
                try { await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); } catch (OperationCanceledException) { break; }
            }
        }
    }

    private async Task RunOnce(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IReconciliationService>();
        var result = await service.ReconcileAsync(ct);
        logger.LogInformation("Reconciliation completed. Created {Count} discrepancies.", result.CreatedCount);
    }
}