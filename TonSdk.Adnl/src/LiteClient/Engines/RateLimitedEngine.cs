using System;
using System.Threading;
using System.Threading.Tasks;

namespace TonSdk.Adnl.LiteClient.Engines;

/// <summary>
///     Rate-limited engine decorator.
///     Limits query rate to prevent overwhelming the lite server.
/// </summary>
public class RateLimitedEngine : LiteEngineDecorator
{
    readonly SemaphoreSlim rateLimiter;
    readonly int requestsPerSecond;
    readonly Timer resetTimer;

    public RateLimitedEngine(ILiteEngine innerEngine, int requestsPerSecond) : base(innerEngine)
    {
        this.requestsPerSecond = requestsPerSecond;
        rateLimiter = new SemaphoreSlim(requestsPerSecond, requestsPerSecond);
        resetTimer = new Timer(_ => ResetRateLimit(), null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
    }

    public override async Task<byte[]> QueryAsync(
        Func<(byte[] queryId, byte[] data)> encoder,
        int timeout = 30000,
        CancellationToken cancellationToken = default)
    {
        await rateLimiter.WaitAsync(cancellationToken);
        return await base.QueryAsync(encoder, timeout, cancellationToken);
    }

    void ResetRateLimit()
    {
        int currentCount = rateLimiter.CurrentCount;
        int toRelease = requestsPerSecond - currentCount;
        if (toRelease > 0)
            rateLimiter.Release(toRelease);
    }

    public override void Dispose()
    {
        resetTimer.Dispose();
        rateLimiter.Dispose();
        base.Dispose();
    }
}