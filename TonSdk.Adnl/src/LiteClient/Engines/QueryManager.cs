using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TonSdk.Core.Crypto;

namespace TonSdk.Adnl.LiteClient.Engines;

/// <summary>
///     Manages pending queries, timeouts, and response matching.
///     Thread-safe for concurrent query operations.
/// </summary>
internal class QueryManager
{
    readonly ConcurrentDictionary<string, QueryContext> pendingQueries = new();

    public int PendingCount => pendingQueries.Count;

    public Task<byte[]> RegisterQuery(byte[] queryId, byte[] packet, int timeoutMs, CancellationToken cancellationToken)
    {
        string id = Utils.BytesToHex(queryId);
        TaskCompletionSource<byte[]> tcs = new();
        CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeoutMs);

        QueryContext context = new()
        {
            Tcs = tcs,
            Packet = packet,
            Registration = cts.Token.Register(() => tcs.TrySetCanceled(cancellationToken))
        };

        pendingQueries.TryAdd(id, context);
        return tcs.Task;
    }

    public bool CompleteQuery(byte[] queryId, byte[] responseData)
    {
        string id = Utils.BytesToHex(queryId);

        if (pendingQueries.TryRemove(id, out QueryContext? context))
        {
            context.Registration.Dispose();
            context.Tcs.TrySetResult(responseData);
            return true;
        }

        return false;
    }

    public bool FailQuery(byte[] queryId, Exception exception)
    {
        string id = Utils.BytesToHex(queryId);

        if (pendingQueries.TryRemove(id, out QueryContext? context))
        {
            context.Registration.Dispose();
            context.Tcs.TrySetException(exception);
            return true;
        }

        return false;
    }

    public byte[][] GetAllPendingPackets()
    {
        byte[][] packets = new byte[pendingQueries.Count][];
        int i = 0;
        foreach (KeyValuePair<string, QueryContext> kvp in pendingQueries)
            packets[i++] = kvp.Value.Packet;
        return packets;
    }

    public void FailAllQueries(Exception exception)
    {
        foreach (KeyValuePair<string, QueryContext> kvp in pendingQueries.ToArray())
            if (pendingQueries.TryRemove(kvp.Key, out QueryContext? context))
            {
                context.Registration.Dispose();
                context.Tcs.TrySetException(exception);
            }
    }

    class QueryContext
    {
        public TaskCompletionSource<byte[]> Tcs { get; init; } = null!;
        public byte[] Packet { get; init; } = null!;
        public CancellationTokenRegistration Registration { get; init; }
    }
}