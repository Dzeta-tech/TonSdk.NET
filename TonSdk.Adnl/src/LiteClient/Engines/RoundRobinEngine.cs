using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TonSdk.Adnl.LiteClient.Engines;

/// <summary>
///     Round-robin engine that distributes queries across multiple engines.
///     Provides load balancing and failover.
/// </summary>
public class RoundRobinEngine : ILiteEngine
{
    readonly ILiteEngine[] engines;
    int currentIndex;

    public RoundRobinEngine(params ILiteEngine[] engines)
    {
        if (engines.Length == 0)
            throw new ArgumentException("At least one engine is required", nameof(engines));

        this.engines = engines;
        
        foreach (ILiteEngine engine in engines)
        {
            engine.Connected += () => Connected?.Invoke();
            engine.Ready += () => Ready?.Invoke();
            engine.Closed += () => Closed?.Invoke();
            engine.Error += (e) => Error?.Invoke(e);
        }
    }

    public bool IsReady => engines.Any(e => e.IsReady);
    public bool IsClosed => engines.All(e => e.IsClosed);

    public event Action? Connected;
    public event Action? Ready;
    public event Action? Closed;
    public event Action<Exception>? Error;

    public async Task<byte[]> QueryAsync(
        Func<(byte[] queryId, byte[] data)> encoder,
        int timeout = 30000,
        CancellationToken cancellationToken = default)
    {
        int attempts = 0;
        Exception? lastException = null;

        while (attempts < engines.Length)
        {
            int index = Interlocked.Increment(ref currentIndex) % engines.Length;
            ILiteEngine engine = engines[index];

            if (!engine.IsReady || engine.IsClosed)
            {
                attempts++;
                continue;
            }

            try
            {
                return await engine.QueryAsync(encoder, timeout, cancellationToken);
            }
            catch (Exception ex)
            {
                lastException = ex;
                attempts++;
            }
        }

        throw lastException ?? new InvalidOperationException("No engines available");
    }

    public void Dispose()
    {
        foreach (ILiteEngine engine in engines)
            engine.Dispose();
    }
}

