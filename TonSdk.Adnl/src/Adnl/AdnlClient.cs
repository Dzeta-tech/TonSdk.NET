using System;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace TonSdk.Adnl
{
    public enum AdnlClientState
    {
        Connecting,
        Open,
        Closing,
        Closed
    }

    public class AdnlClientTcp
    {
        readonly AdnlAddress _address;
        readonly string _host;
        readonly int _port;
        readonly TcpClient _socket;

        // private List<byte> _buffer;
        byte[] _buffer;
        Cipher _cipher;
        Decipher _decipher;
        AdnlKeys _keys;
        NetworkStream _networkStream;
        AdnlAesParams _params;

        public AdnlClientTcp(int host, int port, byte[] peerPublicKey)
        {
            _host = ConvertToIPAddress(host);
            _port = port;
            _address = new AdnlAddress(peerPublicKey);
            _socket = new TcpClient();
            _socket.ReceiveBufferSize = 1 * 1024 * 1024;
            _socket.SendBufferSize = 1 * 1024 * 1024;
        }

        public AdnlClientTcp(int host, int port, string peerPublicKey)
        {
            _host = ConvertToIPAddress(host);
            _port = port;
            _address = new AdnlAddress(peerPublicKey);
            _socket = new TcpClient();
            _socket.ReceiveBufferSize = 1 * 1024 * 1024;
            _socket.SendBufferSize = 1 * 1024 * 1024;
        }

        public AdnlClientTcp(string host, int port, byte[] peerPublicKey)
        {
            _host = host;
            _port = port;
            _address = new AdnlAddress(peerPublicKey);
            _socket = new TcpClient();
            _socket.ReceiveBufferSize = 1 * 1024 * 1024;
            _socket.SendBufferSize = 1 * 1024 * 1024;
        }

        public AdnlClientTcp(string host, int port, string peerPublicKey)
        {
            _host = host;
            _port = port;
            _address = new AdnlAddress(peerPublicKey);
            _socket = new TcpClient();
            _socket.ReceiveBufferSize = 1 * 1024 * 1024;
            _socket.SendBufferSize = 1 * 1024 * 1024;
        }

        public AdnlClientState State { get; private set; } = AdnlClientState.Closed;

        public event Action Connected;
        public event Action Ready;
        public event Action Closed;
        public event Action<byte[]> DataReceived;
        public event Action<Exception> ErrorOccurred;

        async Task Handshake()
        {
            byte[] key = _keys.Shared.Take(16).Concat(_params.Hash.Skip(16).Take(16)).ToArray();
            byte[] nonce = _params.Hash.Take(4).Concat(_keys.Shared.Skip(20).Take(12)).ToArray();

            Cipher cipher = CipherFactory.CreateCipheriv(key, nonce);

            byte[] payload = cipher.Update(_params.Bytes).ToArray();
            byte[] packet = _address.Hash.Concat(_keys.Public).Concat(_params.Hash).Concat(payload).ToArray();

            await _networkStream.WriteAsync(packet, 0, packet.Length).ConfigureAwait(false);
        }

        void OnBeforeConnect()
        {
            if (State != AdnlClientState.Closed) return;
            AdnlKeys keys = new AdnlKeys(_address.PublicKey);

            _keys = keys;
            _params = new AdnlAesParams();
            _cipher = CipherFactory.CreateCipheriv(_params.TxKey, _params.TxNonce);
            _decipher = CipherFactory.CreateDecipheriv(_params.RxKey, _params.RxNonce);
            _buffer = Array.Empty<byte>();
            State = AdnlClientState.Connecting;
        }

        async Task ReadDataAsync()
        {
            try
            {
                byte[] buffer = new byte[1024 * 1024];

                while (_socket.Connected)
                {
                    int bytesRead = await _networkStream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
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
            _buffer = _buffer.Concat(Decrypt(data)).ToArray();
            while (_buffer.Length >= AdnlPacket.PacketMinSize)
            {
                AdnlPacket? packet = AdnlPacket.Parse(_buffer);
                if (packet == null) break;

                _buffer = _buffer.Skip(packet.Length).ToArray();

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
                await _socket.ConnectAsync(_host, _port).ConfigureAwait(false);
                _networkStream = _socket.GetStream();
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
            _socket.Close();
            OnClose();
        }

        public async Task Write(byte[] data)
        {
            AdnlPacket packet = new AdnlPacket(data);
            byte[] encrypted = Encrypt(packet.Data);
            await _networkStream.WriteAsync(encrypted, 0, encrypted.Length).ConfigureAwait(false);
        }

        byte[] Encrypt(byte[] data)
        {
            return _cipher.Update(data);
        }

        byte[] Decrypt(byte[] data)
        {
            return _decipher.Update(data);
        }

        static string ConvertToIPAddress(int number)
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
}