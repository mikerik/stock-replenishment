using System.Threading.Channels;

namespace StockReplenishment.StockCheck;

public interface IStockCheckQueue
{
    ValueTask EnqueueAsync(int requestId);
    IAsyncEnumerable<int> ReadAllAsync(CancellationToken cancellationToken);
}

/// <summary>In-memory hand-off from the API to the background worker.</summary>
public class StockCheckQueue : IStockCheckQueue
{
    private readonly Channel<int> _channel = Channel.CreateUnbounded<int>();

    public ValueTask EnqueueAsync(int requestId) => _channel.Writer.WriteAsync(requestId);

    public IAsyncEnumerable<int> ReadAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}