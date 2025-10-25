using System;
using System.Threading;
using System.Threading.Tasks;

namespace TonSdk.Adnl.LiteClient.Engines;

/// <summary>
///     Base class for engine decorators.
///     Allows wrapping engines with additional functionality (rate limiting, logging, etc).
/// </summary>
public abstract class LiteEngineDecorator : ILiteEngine
{
    protected readonly ILiteEngine innerEngine;

    protected LiteEngineDecorator(ILiteEngine innerEngine)
    {
        this.innerEngine = innerEngine;

        innerEngine.Connected += () => Connected?.Invoke();
        innerEngine.Ready += () => Ready?.Invoke();
        innerEngine.Closed += () => Closed?.Invoke();
        innerEngine.Error += e => Error?.Invoke(e);
    }

    public virtual bool IsReady => innerEngine.IsReady;
    public virtual bool IsClosed => innerEngine.IsClosed;

    public event Action? Connected;
    public event Action? Ready;
    public event Action? Closed;
    public event Action<Exception>? Error;

    public virtual Task<byte[]> QueryAsync(
        Func<(byte[] queryId, byte[] data)> encoder,
        int timeout = 30000,
        CancellationToken cancellationToken = default)
    {
        return innerEngine.QueryAsync(encoder, timeout, cancellationToken);
    }

    public virtual void Dispose()
    {
        innerEngine.Dispose();
    }
}