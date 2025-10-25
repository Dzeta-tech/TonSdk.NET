using System;
using System.Threading;
using System.Threading.Tasks;

namespace TonSdk.Adnl.LiteClient;

/// <summary>
///     Interface for lite server engines.
///     Engines handle the low-level connection and query execution.
///     Implementations can provide single connections, round-robin pools, rate limiting, etc.
/// </summary>
public interface ILiteEngine : IDisposable
{
    /// <summary>
    ///     Check if the engine is ready to accept queries.
    /// </summary>
    bool IsReady { get; }

    /// <summary>
    ///     Check if the engine is closed.
    /// </summary>
    bool IsClosed { get; }

    /// <summary>
    ///     Execute a query against the lite server.
    /// </summary>
    /// <param name="encoder">Function to encode the query</param>
    /// <param name="timeout">Query timeout in milliseconds</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Raw response buffer</returns>
    Task<byte[]> QueryAsync(
        Func<(byte[] queryId, byte[] data)> encoder,
        int timeout = 30000,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Event fired when connection is established.
    /// </summary>
    event Action? Connected;

    /// <summary>
    ///     Event fired when engine is ready to accept queries.
    /// </summary>
    event Action? Ready;

    /// <summary>
    ///     Event fired when connection is closed.
    /// </summary>
    event Action? Closed;

    /// <summary>
    ///     Event fired when an error occurs.
    /// </summary>
    event Action<Exception>? Error;
}