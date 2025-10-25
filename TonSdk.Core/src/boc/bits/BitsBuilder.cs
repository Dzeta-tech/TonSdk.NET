using System;
using System.Collections;
using System.Linq;
using System.Numerics;
using System.Text;

namespace TonSdk.Core.Boc;

public abstract class BitsBuilderImpl<T, TU>(BitArray bits, int cnt)
    where T : BitsBuilderImpl<T, TU>
{
    protected int BitsCnt = cnt;

    protected BitArray _Data = bits;

    public BitsBuilderImpl(int length = 1023) : this(new BitArray(length), 0)
    {
    }

    public int Length => _Data.Length;

    public int RemainderBits => _Data.Length - BitsCnt;

    public Bits Data => new(BitArrayUtils.Slice(_Data, 0, BitsCnt));

    protected void CheckBitsOverflow(Bits bits)
    {
        if (bits.Length > RemainderBits) throw new ArgumentException("Bits overflow");
    }

    protected void CheckBitsOverflow(BitArray bits)
    {
        if (bits.Length > RemainderBits) throw new ArgumentException("Bits overflow");
    }

    protected void CheckBitsOverflow(int len)
    {
        if (len > RemainderBits) throw new ArgumentException("Bits overflow");
    }

    public T StoreBits(Bits bits, bool needCheck = true)
    {
        if (needCheck) CheckBitsOverflow(bits);

        Write(bits, BitsCnt);
        BitsCnt += bits.Length;

        return (T)this;
    }

    public T StoreBits(BitArray bits, bool needCheck = true)
    {
        if (needCheck) CheckBitsOverflow(bits);

        Write(bits, BitsCnt);
        BitsCnt += bits.Length;

        return (T)this;
    }

    public T StoreBits(string s, bool needCheck = true)
    {
        Bits bits = new(s);
        return StoreBits(bits, needCheck);
    }

    public T StoreBit(bool b, bool needCheck = true)
    {
        if (needCheck) CheckBitsOverflow(1);
        _Data[BitsCnt++] = b;
        return (T)this;
    }

    public T StoreByte(byte b, bool needCheck = true)
    {
        return StoreUInt(b, 8, needCheck);
    }

    public T StoreBytes(byte[] b, bool needCheck = true)
    {
        Bits bits = new(b);
        return StoreBits(bits, needCheck);
    }

    public T StoreBytes(ReadOnlySpan<byte> b, bool needCheck = true)
    {
        Bits bits = new(b.ToArray());
        return StoreBits(bits, needCheck);
    }

    public T StoreString(string s, bool needCheck = true)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(s);
        return StoreBytes(bytes, needCheck);
    }

    public T StoreUInt(ulong value, int size, bool needCheck = true)
    {
        BigInteger max = new BigInteger(1) << size;
        if (value >= max) throw new ArgumentException("");

        return StoreNumber(value, size, needCheck);
    }

    public T StoreUInt(BigInteger value, int size, bool needCheck = true)
    {
        BigInteger max = new BigInteger(1) << size;
        if (value < 0 || value >= max) throw new ArgumentException("Value is out of range");

        return StoreNumber(value, size, needCheck);
    }

    public T StoreInt(long value, int size, bool needCheck = true)
    {
        BigInteger max = BigInteger.One << (size - 1);
        if (value < -max || value > max) throw new ArgumentException("");

        return StoreNumber(value, size, needCheck);
    }

    public T StoreInt(BigInteger value, int size, bool needCheck = true)
    {
        BigInteger max = BigInteger.One << (size - 1);
        if (value < -max || value > max) throw new ArgumentException("");

        return StoreNumber(value, size, needCheck);
    }

    public T StoreUInt32Le(uint value)
    {
        return StoreBytes(BitConverter.GetBytes(value));
    }

    public T StoreUInt64Le(ulong value)
    {
        return StoreBytes(BitConverter.GetBytes(value));
    }

    T StoreNumber(BigInteger value, int size, bool needCheck)
    {
        return StoreNumberInternal(value.ToByteArray(), size, needCheck);
    }

    T StoreNumber(ulong value, int size, bool needCheck)
    {
        return StoreNumberInternal(BitConverter.GetBytes(value), size, needCheck);
    }

    T StoreNumber(long value, int size, bool needCheck)
    {
        return StoreNumberInternal(BitConverter.GetBytes(value), size, needCheck);
    }

    T StoreNumberInternal(byte[] bytes, int size, bool needCheck)
    {
        if (BitConverter.IsLittleEndian)
            Array.Reverse(bytes);

        BitArray bitArray = new Bits(bytes).Data;
        int change = size - bitArray.Count;

        if (change < 0)
        {
            bool[] boolArray = new bool[bitArray.Count];
            bitArray.CopyTo(boolArray, 0);
            bitArray = new BitArray(boolArray.Skip(-change).ToArray());
        }
        else
        {
            bool sgn = bitArray[0]; // Check sign bit
            BitArray newBitArray = new(size, sgn);
            for (int i = 0; i < bitArray.Count; i++) newBitArray[i + change] = bitArray[i];
            bitArray = newBitArray;
        }

        Bits bits = new(bitArray);
        return StoreBits(bits);
    }

    public T StoreAddress(Address? address)
    {
        if (address == null) return StoreBits(new BitArray(2, false));

        CheckBitsOverflow(267);
        StoreUInt(0b100, 3);
        StoreInt(address.Value.Workchain, 8);
        StoreBytes(address.Value.Hash);
        return (T)this;
    }

    public T StoreCoins(Coins coins)
    {
        return StoreVarUInt(coins.ToBigInt(), 16);
    }

    public T StoreVarUInt(BigInteger value, int length)
    {
        return StoreVarInt(value, length, false);
    }

    public T StoreVarInt(BigInteger value, int length)
    {
        return StoreVarInt(value, length, true);
    }

    protected T StoreVarInt(BigInteger value, int length, bool sgn)
    {
        int size = (int)Math.Ceiling(Math.Log(length, 2));
        int sizeBytes = (int)Math.Ceiling(BigInteger.Log(value, 2) / 8);
        if (sizeBytes == 0) sizeBytes = 1;
        int sizeBits = sizeBytes * 8;
        CheckBitsOverflow(sizeBits + size);
        return value == 0
            ? StoreUInt(0, size)
            : sgn
                ? StoreUInt((uint)sizeBytes, size).StoreInt(value, sizeBits)
                : StoreUInt((uint)sizeBytes, size).StoreUInt(value, sizeBits);
    }

    public T StoreBitsSlice(BitsSlice bs)
    {
        CheckBitsOverflow(bs.RemainderBits);
        return StoreBits(bs.Bits);
    }

    public abstract T Clone();

    public abstract TU Build();


    protected void Write(Bits newBits, int offset)
    {
        Write(newBits.Data, offset);
    }

    protected void Write(BitArray newBits, int offset)
    {
        // var _newBits = (BitArray)newBits.Clone();
        // _newBits.Length = _Data.Length;
        // _newBits.LeftShift(offset);
        // _Data.Or(_newBits);
        for (int i = 0; i < newBits.Length; i++) _Data[i + offset] = newBits[i];
    }
}

public class BitsBuilder : BitsBuilderImpl<BitsBuilder, Bits>
{
    public BitsBuilder(int length = 1023) : base(length)
    {
    }

    BitsBuilder(BitArray bits, int cnt) : base(bits, cnt)
    {
    }

    public override BitsBuilder Clone()
    {
        return new BitsBuilder((BitArray)_Data.Clone(), BitsCnt);
    }

    public override Bits Build()
    {
        return Data;
    }
}