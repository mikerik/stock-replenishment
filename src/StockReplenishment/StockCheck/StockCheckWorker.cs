namespace StockReplenishment.StockCheck;

/// <summary>
/// Background service: the submit endpoint returns immediately (202 Accepted) and this worker performs
/// the slow check. Up to four checks run in parallel.
/// </summary>
public class StockCheckWorker(IStockCheckQueue queue, IServiceScopeFactory scopeFactory, ILogger<StockCheckWorker> logger)
    : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        Parallel.ForEachAsync(
            queue.ReadAllAsync(stoppingToken),
            new ParallelOptions { MaxDegreeOfParallelism = 4, CancellationToken = stoppingToken },
            async (requestId, cancellationToken) =>
            {
                try
                {
                    // The worker is a singleton, DbContext is scoped: create a scope per check.
                    await using var scope = scopeFactory.CreateAsyncScope();
                    await scope.ServiceProvider.GetRequiredService<StockCheckProcessor>().ProcessAsync(requestId, cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError(ex, "Unexpected error checking request {RequestId}", requestId);
                }
            });
}