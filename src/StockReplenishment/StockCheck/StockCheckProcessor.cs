using Microsoft.EntityFrameworkCore;
using StockReplenishment.Data;

namespace StockReplenishment.StockCheck;

/// <summary>Runs the external check for one request and stores the result.</summary>
public class StockCheckProcessor(AppDbContext db, IStockAvailabilityService stockService, ILogger<StockCheckProcessor> logger)
{
    public async Task ProcessAsync(int requestId, CancellationToken cancellationToken)
    {
        var request = await db.Requests.Include(r => r.Lines).FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken);
        if (request is null)
        {
            return;
        }

        try
        {
            var availability = await stockService.GetAvailabilityAsync(request.Lines.Select(l => l.ArticleNumber), cancellationToken);
            request.CompleteStockCheck(availability);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Stock check failed for request {RequestId}", requestId);
            request.FailStockCheck("The warehouse service could not be reached. Please try again later.");
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}