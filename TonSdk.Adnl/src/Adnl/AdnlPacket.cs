using System;
using System.Linq;
using System.Security.Cryptography;
using TonSdk.Core.Boc;

namespace TonSdk.Adnl;

internal class AdnlPacket
{
    internal const byte packetMinSize = 68; // 4 (size) + 32 (nonce) + 32 (hash)

    internal AdnlPacket(byte[] payload, byte[]? nonce = null)
    {
        Nonce = nonce ?? AdnlKeys.GenerateRandomBytes(32);
        Payload = payload;
    }

    internal byte[] Payload { get; }

    byte[] Nonce { get; }

    byte[] Hash
    {
        get
        {
            using SHA256 sha256 = SHA256.Create();
            byte[] sha256Hash = sha256.ComputeHash(Nonce.Concat(Payload).ToArray());
            return sha256Hash;
        }
    }

    byte[] Size
    {
        get
        {
            uint size = (uint)(32 + 32 + Payload.Length);
            Bits builder = new BitsBuilder().StoreUInt32Le(size).Build();
            return builder.ToBytes();
        }
    }

    internal byte[] Data => Size.Concat(Nonce).Concat(Payload).Concat(Hash).ToArray();

    internal int Length => packetMinSize + Payload.Length;

    internal static AdnlPacket? Parse(byte[] data)
    {
        if (data.Length < 4) return null;
        int cursor = 0;

        BitsSlice slice = new Bits(data).Parse();

        uint size = (uint)slice.LoadUInt32Le();
        cursor += 4;

        if (data.Length - 4 < size) return null;

        byte[] nonce = new byte[32];
        Array.Copy(data, cursor, nonce, 0, 32);
        cursor += 32;

        byte[] payload = new byte[size - (32 + 32)];
        Array.Copy(data, cursor, payload, 0, size - (32 + 32));
        cursor += (int)size - (32 + 32);

        byte[] hash = new byte[32];
        Array.Copy(data, cursor, hash, 0, 32);

        using SHA256 sha256 = SHA256.Create();
        byte[] target = sha256.ComputeHash(nonce.Concat(payload).ToArray());

        if (!hash.SequenceEqual(target)) throw new Exception("ADNLPacket: Bad packet hash.");

        return new AdnlPacket(payload, nonce);
    }
}