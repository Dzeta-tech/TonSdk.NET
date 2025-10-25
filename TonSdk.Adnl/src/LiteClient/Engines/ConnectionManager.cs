using System;
using System.Threading;
using System.Threading.Tasks;
using TonSdk.Adnl.Adnl;

namespace TonSdk.Adnl.LiteClient.Engines;

/// <summary>
///     Manages connection lifecycle, state, and reconnection logic.
///     Separates connection concerns from query handling.
/// </summary>
internal class ConnectionManager(string host, int port, byte[] publicKey, int reconnectDelayMs = 10000)
{
    readonly SemaphoreSlim connectionLock = new(1, 1);

    volatile bool isClosed;
    volatile bool isConnecting;
    volatile bool isReady;

    public bool IsReady => isReady;
    public bool IsClosed => isClosed;
    public AdnlClientTcp? CurrentClient { get; private set; }

    public event Action? Connected;
    public event Action? Ready;
    public event Action? Closed;
    public event Action<byte[]>? DataReceived;

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (isClosed)
            throw new ObjectDisposedException(nameof(ConnectionManager));

        await connectionLock.WaitAsync(cancellationToken);
        try
        {
            if (isReady) return;

            isConnecting = true;

            // Cleanup old client
            if (CurrentClient != null)
            {
                CurrentClient.DataReceived -= OnDataReceived;
                CurrentClient.Closed -= OnClientClosed;
                CurrentClient.End();
            }

            // Create and connect new client
            AdnlClientTcp client = new(host, port, publicKey);
            client.DataReceived += OnDataReceived;
            client.Closed += OnClientClosed;

            await client.Connect();
            await WaitForOpenStateAsync(client, cancellationToken);

            CurrentClient = client;
            isReady = true;

            Connected?.Invoke();
            Ready?.Invoke();
        }
        finally
        {
            isConnecting = false;
            connectionLock.Release();
        }
    }

    public async Task EnsureConnectedAsync(CancellationToken cancellationToken = default)
    {
        if (isReady) return;

        if (isConnecting)
        {
            // Wait for ongoing connection
            DateTime timeout = DateTime.UtcNow.AddSeconds(10);
            while (isConnecting && !isReady && DateTime.UtcNow < timeout)
                await Task.Delay(100, cancellationToken);

            if (isReady) return;
            throw new TimeoutException("Timeout waiting for connection");
        }

        await ConnectAsync(cancellationToken);
    }

    public async Task WriteAsync(byte[] packet)
    {
        if (CurrentClient == null || !isReady)
            throw new InvalidOperationException("Not connected");

        await CurrentClient.Write(packet);
    }

    public void Close()
    {
        isClosed = true;
        isReady = false;
        CurrentClient?.End();
        connectionLock.Dispose();
    }

    void OnDataReceived(byte[] data)
    {
        DataReceived?.Invoke(data);
    }

    void OnClientClosed()
    {
        isReady = false;
        Closed?.Invoke();

        if (!isClosed)
            _ = Task.Run(async () =>
            {
                await Task.Delay(reconnectDelayMs);
                if (!isClosed)
                    await ConnectAsync();
            });
    }

    static async Task WaitForOpenStateAsync(AdnlClientTcp client, CancellationToken cancellationToken)
    {
        DateTime timeout = DateTime.UtcNow.AddSeconds(10);
        while (client.State != AdnlClientState.Open && DateTime.UtcNow < timeout)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(100, cancellationToken);
        }

        if (client.State != AdnlClientState.Open)
            throw new TimeoutException("Connection timeout");
    }
}