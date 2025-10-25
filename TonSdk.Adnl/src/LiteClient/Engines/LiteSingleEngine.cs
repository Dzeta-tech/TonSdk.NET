using System;
using System.Threading;
using System.Threading.Tasks;

namespace TonSdk.Adnl.LiteClient.Engines;

/// <summary>
///     Single connection lite engine.
///     Clean, modular implementation with separated concerns.
/// </summary>
public class LiteSingleEngine : ILiteEngine
{
    readonly ConnectionManager connection;
    readonly QueryManager queries;

    public LiteSingleEngine(string host, int port, byte[] publicKey, int reconnectTimeoutMs = 10000)
    {
        connection = new ConnectionManager(host, port, publicKey, reconnectTimeoutMs);
        queries = new QueryManager();

        connection.Connected += () => Connected?.Invoke();
        connection.Ready += OnReady;
        connection.Closed += OnClosed;
        connection.DataReceived += OnDataReceived;

        _ = Task.Run(async () => await connection.ConnectAsync());
    }

    public event Action? Connected;
    public event Action? Ready;
    public event Action? Closed;
    public event Action<Exception>? Error;

    public bool IsReady => connection.IsReady;
    public bool IsClosed => connection.IsClosed;

    public async Task<byte[]> QueryAsync(
        Func<(byte[] queryId, byte[] data)> encoder,
        int timeout = 30000,
        CancellationToken cancellationToken = default)
    {
        if (connection.IsClosed)
            throw new ObjectDisposedException(nameof(LiteSingleEngine));

        await connection.EnsureConnectedAsync(cancellationToken);

        (byte[] queryId, byte[] packet) = encoder();
        Task<byte[]> queryTask = queries.RegisterQuery(queryId, packet, timeout, cancellationToken);

        await connection.WriteAsync(packet);
        return await queryTask;
    }

    public void Dispose()
    {
        queries.FailAllQueries(new ObjectDisposedException(nameof(LiteSingleEngine)));
        connection.Close();
    }

    void OnReady()
    {
        Ready?.Invoke();

        // Resend pending queries after reconnection
        foreach (byte[] packet in queries.GetAllPendingPackets())
            _ = connection.WriteAsync(packet);
    }

    void OnDataReceived(byte[] data)
    {
        (byte[] queryId, byte[] response)? parsed = ResponseParser.Parse(data);
        if (!parsed.HasValue) return;

        (byte[] queryId, byte[] response) = parsed.Value;
        queries.CompleteQuery(queryId, response);
    }

    void OnClosed()
    {
        Closed?.Invoke();
        queries.FailAllQueries(new InvalidOperationException("Connection closed"));
    }
}