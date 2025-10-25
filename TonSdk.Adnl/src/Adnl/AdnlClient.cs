using System;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace TonSdk.Adnl.Adnl;

public enum AdnlClientState
{
    Connecting,
    Open,
    Closing,
    Closed
}

public class AdnlClientTcp
{
    readonly AdnlAddress address;
    readonly string host;
    readonly int port;
    readonly TcpClient socket;

    // private List<byte> _buffer;
    byte[] buffer;
    Cipher cipher;
    Decipher decipher;
    AdnlKeys keys;
    NetworkStream networkStream;
    AdnlAesParams @params;

    public AdnlClientTcp(int host, int port, byte[] peerPublicKey)
    {
        this.host = ConvertToIpAddress(host);
        this.port = port;
        address = new AdnlAddress(peerPublicKey);
        socket = new TcpClient();
        socket.ReceiveBufferSize = 1 * 1024 * 1024;
        socket.SendBufferSize = 1 * 1024 * 1024;
    }

    public AdnlClientTcp(int host, int port, string peerPublicKey)
    {
        this.host = ConvertToIpAddress(host);
        this.port = port;
        address = new AdnlAddress(peerPublicKey);
        socket = new TcpClient();
        socket.ReceiveBufferSize = 1 * 1024 * 1024;
        socket.SendBufferSize = 1 * 1024 * 1024;
    }

    public AdnlClientTcp(string host, int port, byte[] peerPublicKey)
    {
        this.host = host;
        this.port = port;
        address = new AdnlAddress(peerPublicKey);
        socket = new TcpClient();
        socket.ReceiveBufferSize = 1 * 1024 * 1024;
        socket.SendBufferSize = 1 * 1024 * 1024;
    }

    public AdnlClientTcp(string host, int port, string peerPublicKey)
    {
        this.host = host;
        this.port = port;
        address = new AdnlAddress(peerPublicKey);
        socket = new TcpClient();
        socket.ReceiveBufferSize = 1 * 1024 * 1024;
        socket.SendBufferSize = 1 * 1024 * 1024;
    }

    public AdnlClientState State { get; private set; } = AdnlClientState.Closed;

    public event Action Connected;
    public event Action Ready;
    public event Action Closed;
    public event Action<byte[]> DataReceived;
    public event Action<Exception> ErrorOccurred;

    async Task Handshake()
    {
        byte[] key = keys.Shared.Take(16).Concat(@params.Hash.Skip(16).Take(16)).ToArray();
        byte[] nonce = @params.Hash.Take(4).Concat(keys.Shared.Skip(20).Take(12)).ToArray();

        Cipher cipher = CipherFactory.CreateCipheriv(key, nonce);

        byte[] payload = cipher.Update(@params.Bytes).ToArray();
        byte[] packet = address.Hash.Concat(keys.Public).Concat(@params.Hash).Concat(payload).ToArray();

        await networkStream.WriteAsync(packet, 0, packet.Length).ConfigureAwait(false);
    }

    void OnBeforeConnect()
    {
        if (State != AdnlClientState.Closed) return;
        AdnlKeys keys = new(address.PublicKey);

        this.keys = keys;
        @params = new AdnlAesParams();
        cipher = CipherFactory.CreateCipheriv(@params.TxKey, @params.TxNonce);
        decipher = CipherFactory.CreateDecipheriv(@params.RxKey, @params.RxNonce);
        buffer = Array.Empty<byte>();
        State = AdnlClientState.Connecting;
    }

    async Task ReadDataAsync()
    {
        try
        {
            byte[] buffer = new byte[1024 * 1024];

            while (socket.Connected)
            {
                int bytesRead = await networkStream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                if (bytesRead > 0)
                {
                    byte[] receivedData = new byte[bytesRead];
                    Array.Copy(buffer, receivedData, bytesRead);

                    OnDataReceived(receivedData);
                }
                else if (bytesRead == 0)
                {
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(ex);
        }
        finally
        {
            OnClose();
        }
    }

    void OnReady()
    {
        State = AdnlClientState.Open;
        Ready?.Invoke();
    }

    void OnClose()
    {
        State = AdnlClientState.Closed;
        Closed?.Invoke();
    }

    void OnDataReceived(byte[] data)
    {
        buffer = buffer.Concat(Decrypt(data)).ToArray();
        while (buffer.Length >= AdnlPacket.packetMinSize)
        {
            AdnlPacket? packet = AdnlPacket.Parse(buffer);
            if (packet == null) break;

            buffer = buffer.Skip(packet.Length).ToArray();

            if (State == AdnlClientState.Connecting)
            {
                if (packet.Payload.Length != 0)
                {
                    ErrorOccurred?.Invoke(new Exception("AdnlClient: Bad handshake."));
                    End();
                    State = AdnlClientState.Closed;
                }
                else
                {
                    OnReady();
                }

                break;
            }

            DataReceived?.Invoke(packet.Payload);
        }
    }

    public async Task Connect()
    {
        OnBeforeConnect();
        try
        {
            await socket.ConnectAsync(host, port).ConfigureAwait(false);
            networkStream = socket.GetStream();
            Task.Run(async () => await ReadDataAsync().ConfigureAwait(false));
            Connected?.Invoke();
            await Handshake().ConfigureAwait(false);
        }
        catch (Exception e)
        {
            ErrorOccurred?.Invoke(e);
            End();
            State = AdnlClientState.Closed;
        }
    }

    public void End()
    {
        if (State == AdnlClientState.Closed || State == AdnlClientState.Closing) return;
        socket.Close();
        OnClose();
    }

    public async Task Write(byte[] data)
    {
        AdnlPacket packet = new(data);
        byte[] encrypted = Encrypt(packet.Data);
        await networkStream.WriteAsync(encrypted, 0, encrypted.Length).ConfigureAwait(false);
    }

    byte[] Encrypt(byte[] data)
    {
        return cipher.Update(data);
    }

    byte[] Decrypt(byte[] data)
    {
        return decipher.Update(data);
    }

    static string ConvertToIpAddress(int number)
    {
        uint unsignedNumber = (uint)number;
        byte[] bytes = new byte[4];

        bytes[0] = (byte)((unsignedNumber >> 24) & 0xFF);
        bytes[1] = (byte)((unsignedNumber >> 16) & 0xFF);
        bytes[2] = (byte)((unsignedNumber >> 8) & 0xFF);
        bytes[3] = (byte)(unsignedNumber & 0xFF);

        return $"{bytes[0]}.{bytes[1]}.{bytes[2]}.{bytes[3]}";
    }
}