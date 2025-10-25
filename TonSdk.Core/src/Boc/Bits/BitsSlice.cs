using System;
using System.Collections;
using System.Numerics;
using System.Text;
using TonSdk.Core.Addresses;
using TonSdk.Core.Economics;

namespace TonSdk.Core.boc.bits;

public abstract class BitsSliceImpl<T, TU> where T : BitsSliceImpl<T, TU>
{
    protected Bits __Bits;
    protected int BitsEn;
    protected int BitsSt;

    public BitsSliceImpl(BitArray bits)
    {
        __Bits = new Bits(bits);
        BitsEn = __Bits.Length;
    }

    public BitsSliceImpl(Bits bits)
    {
        __Bits = bits;
        BitsEn = __Bits.Length;
    }

    protected BitsSliceImpl(Bits bits, int bitsSt, int bitsEn)
    {
        __Bits = bits;
        BitsSt = bitsSt;
        BitsEn = bitsEn;
    }

    public int RemainderBits => BitsEn - BitsSt;

    public Bits Bits => new(BitArrayUtils.Slice(__Bits.Data, BitsSt, BitsEn));

    protected void CheckBitsUnderflow(int bitEnd)
    {
        if (bitEnd > BitsEn) throw new ArgumentException("Bits underflow");
    }

    protected bool CheckBitsUnderflowQ(int bitEnd)
    {
        return bitEnd > BitsEn;
    }

    protected void CheckSize(int size)
    {
        if (size < 0) throw new ArgumentException("Invalid size. Must be >= 0", nameof(size));
    }

    public T SkipBits(int size)
    {
        CheckSize(size);
        int bitEnd = BitsSt + size;
        CheckBitsUnderflow(bitEnd);
        BitsSt = bitEnd;
        return (T)this;
    }

    public Bits ReadBits(int size)
    {
        CheckSize(size);
        int bitEnd = BitsSt + size;
        CheckBitsUnderflow(bitEnd);
        return new Bits(BitArrayUtils.Slice(__Bits.Data, BitsSt, bitEnd));
    }

    public Bits LoadBits(int size)
    {
        CheckSize(size);
        int bitEnd = BitsSt + size;
        CheckBitsUnderflow(bitEnd);
        BitArray bits = BitArrayUtils.Slice(__Bits.Data, BitsSt, bitEnd);
        BitsSt = bitEnd;
        return new Bits(bits);
    }

    public T SkipBit()
    {
        return SkipBits(1);
    }

    public bool ReadBit()
    {
        int bitEnd = BitsSt + 1;
        CheckBitsUnderflow(bitEnd);
        return __Bits.Data[BitsSt];
    }

    public bool ReadBit(int idx)
    {
        int bitEnd = BitsSt + idx + 1;
        CheckBitsUnderflow(bitEnd);
        return __Bits.Data[BitsSt + idx];
    }

    public bool LoadBit()
    {
        int bitEnd = BitsSt + 1;
        CheckBitsUnderflow(bitEnd);
        bool bit = __Bits.Data[BitsSt];
        BitsSt = bitEnd;
        return bit;
    }

    public BigInteger ReadUInt(int size)
    {
        CheckSize(size);
        int bitEnd = BitsSt + size;
        CheckBitsUnderflow(bitEnd);
        return _unsafeReadBigInteger(size);
    }

    public BigInteger LoadUInt(int size)
    {
        CheckSize(size);
        int bitEnd = BitsSt + size;
        CheckBitsUnderflow(bitEnd);
        BigInteger result = _unsafeReadBigInteger(size);
        BitsSt = bitEnd;
        return result;
    }

    public BigInteger ReadInt(int size)
    {
        CheckSize(size);
        int bitEnd = BitsSt + size;
        CheckBitsUnderflow(bitEnd);
        return _unsafeReadBigInteger(size, true);
    }

    public BigInteger LoadInt(int size)
    {
        CheckSize(size);
        int bitEnd = BitsSt + size;
        CheckBitsUnderflow(bitEnd);
        BigInteger result = _unsafeReadBigInteger(size, true);
        BitsSt = bitEnd;
        return result;
    }

    public BigInteger ReadUInt32Le()
    {
        int bitEnd = BitsSt + 32;
        CheckBitsUnderflow(bitEnd);
        return _unsafeReadBigInteger(32, false, true);
    }

    public BigInteger LoadUInt32Le()
    {
        int bitEnd = BitsSt + 32;
        CheckBitsUnderflow(bitEnd);
        BigInteger result = _unsafeReadBigInteger(32, false, true);
        BitsSt = bitEnd;
        return result;
    }

    public BigInteger ReadUInt64Le()
    {
        int bitEnd = BitsSt + 64;
        CheckBitsUnderflow(bitEnd);
        return _unsafeReadBigInteger(64, false, true);
    }

    public BigInteger LoadUInt64Le()
    {
        int bitEnd = BitsSt + 64;
        CheckBitsUnderflow(bitEnd);
        BigInteger result = _unsafeReadBigInteger(64, false, true);
        BitsSt = bitEnd;
        return result;
    }

    public Coins ReadCoins(int decimals = 9)
    {
        return new Coins((decimal)ReadVarUInt(16), new CoinsOptions(true, decimals));
    }

    public Coins LoadCoins(int decimals = 9)
    {
        return new Coins((decimal)LoadVarUInt(16), new CoinsOptions(true, decimals));
    }

    public BigInteger ReadVarUInt(int length)
    {
        return LoadVarInt(length, false, false);
    }

    public BigInteger LoadVarUInt(int length)
    {
        return LoadVarInt(length, false, true);
    }

    public BigInteger ReadVarInt(int length)
    {
        return LoadVarInt(length, true, false);
    }

    public BigInteger LoadVarInt(int length)
    {
        return LoadVarInt(length, true, true);
    }

    protected BigInteger LoadVarInt(int length, bool sgn, bool inplace)
    {
        int size = (int)Math.Ceiling(Math.Log(length, 2));
        int sizeBytes = (int)ReadUInt(size);
        int sizeBits = sizeBytes * 8;

        CheckBitsUnderflow(BitsSt + size + sizeBits);

        if (inplace)
        {
            SkipBits(size);
            return sizeBits == 0
                ? BigInteger.Zero
                : sgn
                    ? LoadInt(sizeBits)
                    : LoadUInt(sizeBits);
        }

        BitsSlice varIntSlice = ReadBits(size + sizeBits).Parse();
        varIntSlice.SkipBits(size);
        return sizeBits == 0
            ? BigInteger.Zero
            : sgn
                ? varIntSlice.LoadInt(sizeBits)
                : varIntSlice.LoadUInt(sizeBits);
    }

    public Address? ReadAddress()
    {
        return LoadAddress(false);
    }

    public Address? LoadAddress()
    {
        return LoadAddress(true);
    }

    protected Address? LoadAddress(bool inplace)
    {
        byte prefix = (byte)ReadUInt(2);
        switch (prefix)
        {
            case 0b10: // addr_std
                byte prefixAndAnycast = (byte)ReadUInt(3);
                if (prefixAndAnycast == 0b101)
                    throw new AddressTypeNotSupportedError("Anycast addresses are not supported");

                CheckBitsUnderflow(BitsSt + 267);
                if (inplace)
                {
                    SkipBits(3);
                    return new Address((int)LoadInt(8), LoadBytes(32));
                }

                BitsSlice addrSlice = ReadBits(267).Parse();
                addrSlice.SkipBits(3);
                return new Address((int)addrSlice.LoadInt(8), addrSlice.LoadBytes(32));
            case 0b01: // addr_extern
            {
                int len = (int)LoadInt(9);
                Bits extAddr = LoadBits(len);
                ExternalAddress address = new(len, extAddr);
                return null;
            }
            case 0b11: // addr_var
                throw new AddressTypeNotSupportedError("Var addresses are not supported");
            default: // addr_none
                if (inplace) SkipBits(2);
                return null;
        }
    }

    public byte[] ReadBytes(int size)
    {
        return ReadBits(size * 8).ToBytes();
    }

    public byte[] LoadBytes(int size)
    {
        return LoadBits(size * 8).ToBytes();
    }

    public string ReadString()
    {
        return ReadString(RemainderBits / 8);
    }

    public string ReadString(int size)
    {
        return Encoding.UTF8.GetString(ReadBytes(size));
    }

    public string LoadString()
    {
        return LoadString(RemainderBits / 8);
    }

    public string LoadString(int size)
    {
        return Encoding.UTF8.GetString(LoadBytes(size));
    }

    public abstract TU Restore();

    BigInteger _unsafeReadBigInteger(int size, bool sgn = false, bool le = false)
    {
        BigInteger result = 0;

        if (le)
        {
            BitArray bits = new(LoadBits(size).ToBytes());
            for (int i = 0; i < size; i++)
                if (bits[i])
                    result |= BigInteger.One << i;
        }
        else
        {
            for (int i = 0; i < size; i++)
                if (__Bits.Data[BitsSt + i])
                    result |= BigInteger.One << (size - 1 - i);
        }

        // Check if the most significant bit is set (which means the number is negative)
        if (sgn & ((result & (BigInteger.One << (size - 1))) != 0))
            // If the number is negative, apply two's complement
            result -= BigInteger.One << size;

        return result;
    }
}


public class BitsSlice : BitsSliceImpl<BitsSlice, Bits>
{
    public BitsSlice(BitArray bits) : base(bits)
    {
    }

    public BitsSlice(Bits bits) : base(bits)
    {
    }

    public Bits RestoreRemainder()
    {
        return Bits;
    }

    public override Bits Restore()
    {
        return __Bits;
    }
}

public class AddressTypeNotSupportedError : Exception
{
    public AddressTypeNotSupportedError()
    {
    }

    public AddressTypeNotSupportedError(string message)
        : base(message)
    {
    }
}