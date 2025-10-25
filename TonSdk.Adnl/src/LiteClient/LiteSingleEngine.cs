using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using TonSdk.Core;

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
            public required TaskCompletionSource<byte[]> TaskCompletionSource { get; init; }
            public required byte[] Packet { get; init; }
            public required CancellationTokenRegistration CancellationRegistration { get; set; }
        }

        public LiteSingleEngine(string host, int port, byte[] publicKey, int reconnectTimeoutMs = 10000)
        {
            _host = host;
            _port = port;
            _publicKey = publicKey;
            _reconnectTimeoutMs = reconnectTimeoutMs;

            // Start connection immediately in background
            _ = Task.Run(async () =>
            {
                try
                {
                    await ConnectAsync();
                }
                catch
                {
                    // Will retry on first query or via auto-reconnect
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
                await EnsureConnectedAsync(cancellationToken);
            }

            var (queryId, packet) = encoder();
            string queryIdHex = Utils.BytesToHex(queryId);

            var tcs = new TaskCompletionSource<byte[]>();
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeout);

            var context = new QueryContext
            {
                TaskCompletionSource = tcs,
                Packet = packet,
                CancellationRegistration = cts.Token.Register(() => tcs.TrySetCanceled(cancellationToken))
            };

            _pendingQueries.TryAdd(queryIdHex, context);

            try
            {
                // Send query if ready
                if (_isReady && _currentClient != null)
                {
                    await _currentClient.Write(packet);
                }
                else
                {
                    throw new InvalidOperationException("Not connected to lite server");
                }

                return await tcs.Task;
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
                // Parse ADNL message
                var buffer = new TL.TLReadBuffer(data);
                byte[] queryId = buffer.ReadBytes(32);
                string queryIdHex = Utils.BytesToHex(queryId);

                if (_pendingQueries.TryRemove(queryIdHex, out var context))
                {
                    // Read remaining response data (entire remaining buffer)
                    byte[] responseData = buffer.ReadObject();
                    context.TaskCompletionSource.TrySetResult(responseData);
                }
            }
            catch (Exception ex)
            {
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

