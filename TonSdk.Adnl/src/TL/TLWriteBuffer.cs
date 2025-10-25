using System;
using System.IO;
using System.Numerics;
using System.Text;

namespace TonSdk.Adnl.TL;

public class TLWriteBuffer
{
    MemoryStream stream;
    BinaryWriter writer;

    public TLWriteBuffer()
    {
        stream = new MemoryStream(128);
        writer = new BinaryWriter(stream);
    }

    void EnsureSize(int needBytes)
    {
        if (stream.Length - stream.Position < needBytes)
        {
            int newLength = (int)stream.Length * 2;
            MemoryStream newStream = new(newLength);
            stream.Position = 0;
            stream.CopyTo(newStream);
            stream = newStream;
            writer = new BinaryWriter(stream);
        }
    }

    public void WriteInt32(int val)
    {
        EnsureSize(4);
        writer.Write(val);
    }

    public void WriteUInt32(uint val)
    {
        EnsureSize(4);
        writer.Write(val);
    }

    public void WriteInt64(long val)
    {
        EnsureSize(8);
        writer.Write(val);
    }

    public void WriteUInt8(byte val)
    {
        EnsureSize(1);
        writer.Write(val);
    }

    public void WriteInt256(BigInteger val)
    {
        EnsureSize(32);
        Span<byte> buffer = stackalloc byte[32];
        buffer.Clear();
        if (!val.TryWriteBytes(buffer, out int bytesWritten)) throw new Exception("Invalid int256 length");
        if (val < 0 && bytesWritten < 32)
            // two's complement representation
            buffer[bytesWritten..].Fill(0xFF);

        writer.Write(buffer);
    }

    public void WriteBytes(byte[] data, int size)
    {
        if (data.Length != size) throw new Exception($"Input array size not equals to {size}.");
        EnsureSize(size);
        writer.Write(data);
    }

    public void WriteBuffer(byte[] buf)
    {
        EnsureSize(buf.Length + 4);
        int len = 0;
        if (buf.Length <= 253)
        {
            WriteUInt8((byte)buf.Length);
            len += 1;
        }
        else
        {
            WriteUInt8(254);
            EnsureSize(3 + buf.Length);
            byte[] lengthBytes = BitConverter.GetBytes(buf.Length);
            if (!BitConverter.IsLittleEndian) Array.Reverse(lengthBytes);
            writer.Write(lengthBytes, 0, 3);
            len += 4;
        }

        foreach (byte byteVal in buf)
        {
            WriteUInt8(byteVal);
            len += 1;
        }

        while (len % 4 != 0)
        {
            WriteUInt8(0);
            len += 1;
        }
    }

    public void WriteString(string src)
    {
        WriteBuffer(Encoding.UTF8.GetBytes(src));
    }

    public void WriteBool(bool src)
    {
        WriteUInt32(src ? 0x997275b5 : 0xbc799737);
    }

    public void WriteVector<T>(Action<T, TLWriteBuffer> codec, T[] data)
    {
        WriteUInt32((uint)data.Length);
        foreach (T d in data) codec(d, this);
    }

    public byte[] Build()
    {
        return stream.ToArray();
    }
}