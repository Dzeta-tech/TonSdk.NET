using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TonSdk.Core;
using TonSdk.Core.Crypto;

namespace TonSdk.Adnl.LiteClient
{
    /// <summary>
    /// Single connection lite engine.
    /// Maintains one TCP connection to a lite server with automatic reconnection.
    /// Thread-safe and handles reconnection automatically in the background.
    /// </summary>
    public class LiteSingleEngine : ILiteEngine
    {
        readonly string _host;
        readonly int _port;
        readonly byte[] _publicKey;
        readonly int _reconnectTimeoutMs;
        readonly ILogger _logger;
        readonly SemaphoreSlim _connectionLock = new(1, 1);
        readonly ConcurrentDictionary<string, QueryContext> _pendingQueries = new();

        AdnlClientTcp? _currentClient;
        volatile bool _isReady;
        volatile bool _isClosed;
        volatile bool _isConnecting;

        public event Action? Connected;
        public event Action? Ready;
        public event Action? Closed;
        public event Action<Exception>? Error;

        class QueryContext
        {
            public TaskCompletionSource<byte[]> TaskCompletionSource { get; set; } = null!;
            public byte[] Packet { get; set; } = null!;
            public CancellationTokenRegistration CancellationRegistration { get; set; }
        }

        public LiteSingleEngine(string host, int port, byte[] publicKey, int reconnectTimeoutMs = 10000, ILogger? logger = null)
        {
            _host = host;
            _port = port;
            _publicKey = publicKey;
            _reconnectTimeoutMs = reconnectTimeoutMs;
            _logger = logger ?? NullLogger.Instance;

            _logger.LogDebug("LiteSingleEngine initialized for {Host}:{Port}, reconnectTimeout={ReconnectTimeoutMs}ms", 
                _host, _port, _reconnectTimeoutMs);

            // Start connection immediately in background
            _ = Task.Run(async () =>
            {
                try
                {
                    await ConnectAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Initial connection attempt failed, will retry on first query or via auto-reconnect");
                }
            });
        }

        public bool IsReady => _isReady;
        public bool IsClosed => _isClosed;

        public async Task<byte[]> QueryAsync(
            Func<(byte[] queryId, byte[] data)> encoder,
            int timeout = 30000,
            CancellationToken cancellationToken = default)
        {
            if (_isClosed)
                throw new ObjectDisposedException(nameof(LiteSingleEngine));

            // Ensure connected
            if (!_isReady)
            {
                _logger.LogDebug("QueryAsync: Not ready, ensuring connection...");
                await EnsureConnectedAsync(cancellationToken);
            }

            var (queryId, packet) = encoder();
            string queryIdHex = Utils.BytesToHex(queryId);

            _logger.LogDebug("QueryAsync: Sending query {QueryId}, timeout={Timeout}ms, packetSize={PacketSize}bytes", 
                queryIdHex, timeout, packet.Length);

            var tcs = new TaskCompletionSource<byte[]>();
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeout);

            var context = new QueryContext
            {
                TaskCompletionSource = tcs,
                Packet = packet,
                CancellationRegistration = cts.Token.Register(() => 
                {
                    _logger.LogWarning("QueryAsync: Query {QueryId} cancelled/timed out after {Timeout}ms", queryIdHex, timeout);
                    tcs.TrySetCanceled(cancellationToken);
                })
            };

            _pendingQueries.TryAdd(queryIdHex, context);

            try
            {
                // Send query if ready
                if (_isReady && _currentClient != null)
                {
                    await _currentClient.Write(packet);
                    _logger.LogDebug("QueryAsync: Query {QueryId} sent, waiting for response (pending queries: {PendingCount})", 
                        queryIdHex, _pendingQueries.Count);
                }
                else
                {
                    throw new InvalidOperationException("Not connected to lite server");
                }

                var result = await tcs.Task;
                _logger.LogDebug("QueryAsync: Query {QueryId} completed, responseSize={ResponseSize}bytes", 
                    queryIdHex, result.Length);
                return result;
            }
            finally
            {
                if (_pendingQueries.TryRemove(queryIdHex, out var ctx))
                {
                    ctx.CancellationRegistration.Dispose();
                }
                cts.Dispose();
            }
        }

        async Task EnsureConnectedAsync(CancellationToken cancellationToken = default)
        {
            // Fast path: already ready
            if (_isReady)
                return;

            // Wait briefly if someone else is connecting
            if (_isConnecting)
            {
                var timeout = DateTime.UtcNow.AddSeconds(10);
                while (_isConnecting && !_isReady && DateTime.UtcNow < timeout)
                {
                    await Task.Delay(100, cancellationToken);
                }

                if (_isReady)
                    return;

                throw new TimeoutException("Timeout waiting for connection");
            }

            // Connect
            await _connectionLock.WaitAsync(cancellationToken);
            try
            {
                if (_isReady)
                    return;

                await ConnectAsync(cancellationToken);
            }
            finally
            {
                _connectionLock.Release();
            }
        }

        async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            if (_isClosed)
                throw new ObjectDisposedException(nameof(LiteSingleEngine));

            _isConnecting = true;

            try
            {
                // Cleanup old client
                if (_currentClient != null)
                {
                    _currentClient.DataReceived -= OnDataReceived;
                    _currentClient.Closed -= OnClientClosed;
                    _currentClient.End();
                }

                // Create new client
                var client = new AdnlClientTcp(_host, _port, _publicKey);
                client.DataReceived += OnDataReceived;
                client.Closed += OnClientClosed;

                // Connect
                await client.Connect();

                // Wait for ready state
                var timeout = DateTime.UtcNow.AddSeconds(10);
                while (client.State != AdnlClientState.Open && DateTime.UtcNow < timeout)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await Task.Delay(100, cancellationToken);
                }

                if (client.State != AdnlClientState.Open)
                    throw new TimeoutException("Connection timeout");

                _currentClient = client;
                Connected?.Invoke();

                _isReady = true;
                Ready?.Invoke();

                // Resend pending queries
                foreach (var kvp in _pendingQueries)
                {
                    try
                    {
                        await _currentClient.Write(kvp.Value.Packet);
                    }
                    catch
                    {
                        // Query will timeout and be retried by caller
                    }
                }
            }
            catch (Exception ex)
            {
                _isReady = false;
                Error?.Invoke(ex);
                throw;
            }
            finally
            {
                _isConnecting = false;
            }
        }

        void OnDataReceived(byte[] data)
        {
            try
            {
                _logger.LogDebug("OnDataReceived: Received {DataSize}bytes (pending queries: {PendingCount})", 
                    data.Length, _pendingQueries.Count);

                // Parse ADNL message
                var buffer = new TL.TLReadBuffer(data);
                
                // Read TL answer prefix (4 bytes)
                uint tlAnswer = buffer.ReadUInt32();
                _logger.LogDebug("OnDataReceived: TL answer prefix={TLAnswer:X8}", tlAnswer);
                
                // Check if this is a pong response (tcp.pong = 0x0A9276D4)
                if (tlAnswer == 0x0A9276D4)
                {
                    _logger.LogDebug("OnDataReceived: Received pong response, ignoring");
                    return;
                }
                
                // Read query ID (32 bytes)
                byte[] queryId = buffer.ReadBytes(32);
                string queryIdHex = Utils.BytesToHex(queryId);

                _logger.LogDebug("OnDataReceived: Parsed queryId={QueryId}", queryIdHex);

                if (_pendingQueries.TryRemove(queryIdHex, out var context))
                {
                    // Read TL-encoded response buffer (length-prefixed)
                    byte[] liteQuery = buffer.ReadBuffer();
                    _logger.LogDebug("OnDataReceived: Matched query {QueryId}, liteQuerySize={LiteQuerySize}bytes",
                        queryIdHex, liteQuery.Length);
                    
                    // Parse lite server response (has its own TL structure)
                    var liteBuffer = new TL.TLReadBuffer(liteQuery);
                    uint responseCode = liteBuffer.ReadUInt32();
                    
                    _logger.LogDebug("OnDataReceived: Response code={ResponseCode:X8}", responseCode);
                    
                    // Check for liteServer.error (0x4E4F4301 = CRC32 of "liteServer.error code:int message:string = liteServer.Error")
                    if (responseCode == 0x4E4F4301)
                    {
                        int errorCode = liteBuffer.ReadInt32();
                        string errorMessage = liteBuffer.ReadString();
                        var ex = new Exception($"LiteServer error {errorCode}: {errorMessage}");
                        _logger.LogError(ex, "OnDataReceived: LiteServer returned error for query {QueryId}", queryIdHex);
                        context.TaskCompletionSource.TrySetException(ex);
                        return;
                    }
                    
                    // Return the remaining buffer (positioned after response code) as byte[]
                    byte[] responseData = liteBuffer.ReadObject();
                    _logger.LogDebug("OnDataReceived: Response data size={ResponseSize}bytes, completing task", responseData.Length);
                    context.TaskCompletionSource.TrySetResult(responseData);
                }
                else
                {
                    _logger.LogWarning("OnDataReceived: Received response for unknown queryId={QueryId}, pending queries: {PendingQueries}", 
                        queryIdHex, string.Join(", ", _pendingQueries.Keys));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OnDataReceived: Error processing received data");
                Error?.Invoke(ex);
            }
        }

        void OnClientClosed()
        {
            _isReady = false;
            Closed?.Invoke();

            // Fail all pending queries
            foreach (var kvp in _pendingQueries.ToArray())
            {
                if (_pendingQueries.TryRemove(kvp.Key, out var context))
                {
                    context.TaskCompletionSource.TrySetException(
                        new InvalidOperationException("Connection closed"));
                }
            }

            // Auto-reconnect in background
            if (!_isClosed)
            {
                _ = Task.Run(async () =>
                {
                    await Task.Delay(_reconnectTimeoutMs);
                    if (!_isClosed)
                    {
                        try
                        {
                            await ConnectAsync();
                        }
                        catch (Exception ex)
                        {
                            Error?.Invoke(ex);
                        }
                    }
                });
            }
        }

        public void Dispose()
        {
            if (_isClosed)
                return;

            _isClosed = true;
            _isReady = false;

            _currentClient?.End();
            _connectionLock.Dispose();

            // Fail all pending queries
            foreach (var kvp in _pendingQueries.ToArray())
            {
                if (_pendingQueries.TryRemove(kvp.Key, out var context))
                {
                    context.TaskCompletionSource.TrySetException(
                        new ObjectDisposedException(nameof(LiteSingleEngine)));
                }
            }
        }
    }
}

