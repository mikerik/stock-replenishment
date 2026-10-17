namespace StockReplenishment.StockCheck;

/// <summary>The external warehouse system. Returns the available quantity per article number.</summary>
public interface IStockAvailabilityService
{
    Task<Dictionary<string, int>> GetAvailabilityAsync(IEnumerable<string> articleNumbers, CancellationToken cancellationToken);
}

/// <summary>Stand-in for the slow external service: 3–8 seconds, and fails now and then.</summary>
public class SimulatedStockAvailabilityService : IStockAvailabilityService
{
    public async Task<Dictionary<string, int>> GetAvailabilityAsync(IEnumerable<string> articleNumbers, CancellationToken cancellationToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(Random.Shared.Next(3, 9)), cancellationToken);

        if (Random.Shared.NextDouble() < 0.1)
        {
            throw new HttpRequestException("Warehouse service unavailable (simulated).");
        }

        return articleNumbers.Distinct().ToDictionary(a => a, _ => Random.Shared.Next(0, 500));
    }
}