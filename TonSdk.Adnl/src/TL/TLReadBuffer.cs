using System;
using System.IO;
using System.Text;

namespace TonSdk.Adnl.TL;

public class TLReadBuffer(byte[] buffer)
{
    readonly BinaryReader reader = new(new MemoryStream(buffer));

    public int Remaining => (int)(reader.BaseStream.Length - reader.BaseStream.Position);

    void EnsureSize(int needBytes)
    {
        if (reader.BaseStream.Position + needBytes > reader.BaseStream.Length)
            throw new Exception("Not enough bytes");
    }

    public int ReadInt32()
    {
        EnsureSize(4);
        return reader.ReadInt32();
    }

    public uint ReadUInt32()
    {
        EnsureSize(4);
        return reader.ReadUInt32();
    }

    public long ReadInt64()
    {
        EnsureSize(8);
        return reader.ReadInt64();
    }

    public byte ReadUInt8()
    {
        EnsureSize(1);
        return reader.ReadByte();
    }

    public byte[] ReadInt256()
    {
        EnsureSize(32);
        return reader.ReadBytes(32);
    }

    public byte[] ReadBytes(int size)
    {
        EnsureSize(size);
        return reader.ReadBytes(size);
    }


    public byte[] ReadBuffer()
    {
        int len = ReadUInt8();

        if (len == 254)
        {
            byte[] readed = reader.ReadBytes(3);
            len = readed[0] | (readed[1] << 8) | (readed[2] << 16);
        }

        byte[] buffer = reader.ReadBytes(len);

        while (reader.BaseStream.Position % 4 != 0) reader.ReadByte();

        return buffer;
    }

    public string ReadString()
    {
        byte[] buffer = ReadBuffer();
        return Encoding.UTF8.GetString(buffer);
    }

    bool CompareBytes(byte[] array, byte[] compareWith)
    {
        if (array.Length != compareWith.Length) return false;

        for (int i = 0; i < array.Length; i++)
            if (array[i] != compareWith[i])
                return false;

        return true;
    }

    public bool ReadBool()
    {
        byte[] value = ReadBytes(4);

        byte[] falseBytes = { 0x37, 0x97, 0x79, 0xbc };
        byte[] trueBytes = { 0xb5, 0x75, 0x72, 0x99 };

        if (CompareBytes(value, falseBytes)) return false;
        if (CompareBytes(value, trueBytes)) return true;

        throw new Exception("Unknown boolean value");
    }

    public T[] ReadVector<T>(Func<TLReadBuffer, T> codec)
    {
        int count = (int)ReadUInt32();
        T[] result = new T[count];
        for (int i = 0; i < count; i++) result[i] = codec(this);
        return result;
    }

    public byte[] ReadObject()
    {
        int remainingBytes = (int)(reader.BaseStream.Length - reader.BaseStream.Position);
        return reader.ReadBytes(remainingBytes);
    }
}