using System;
using System.Collections;
using System.Security.Cryptography;

namespace TonSdk.Core.Boc;

public class Bits : IComparable<Bits>
{
    static readonly char[] HexSymbols =
    {
        '0', '1', '2', '3', '4', '5', '6', '7',
        '8', '9', 'A', 'B', 'C', 'D', 'E', 'F'
    };


    public Bits(int length = 1023)
    {
        Data = new BitArray(length);
    }

    public Bits(BitArray b)
    {
        Data = b;
    }

    public Bits(string s) : this(FromString(s))
    {
    }

    public Bits(byte[] bytesOrig)
    {
        byte[] bytes = (byte[])bytesOrig.Clone();
        for (int i = 0; i < bytes.Length; i++) bytes[i] = bytes[i].ReverseBits();

        Data = new BitArray(bytes);
    }

    public int Length => Data.Length;

    public BitArray Data { get; }

    public int CompareTo(Bits other)
    {
        // Ensure the BitArrays are the same length
        if (Data.Count != other.Data.Count)
            throw new ArgumentException("BitArrays must be the same length");

        // Compute XOR of the two BitArrays
        BitArray xorResult = new BitArray(Data).Xor(other.Data);

        // Look for the first set bit
        for (int i = 0; i < xorResult.Count; i++)
            if (xorResult[i])
                // If the current bit is true and the other is false, return 1.
                // Otherwise, return -1.
                return Data[i] ? 1 : -1;

        // If all bits are identical, the BitArrays are equal
        return 0;
    }

    public Bits Slice(int start, int end)
    {
        if (end < 0) end = Length + end;
        if (start < 0) start = Length + start;

        if (start < 0 || end < 0 || start > end || start > Length || end > Length)
            throw new ArgumentException("Invalid slice range");

        BitArray ret = new(end - start);
        for (int i = start; i < end; i++) ret[i - start] = Data[i];

        return new Bits(ret);
    }

    public Bits Augment(int divider = 8)
    {
        BitArray bits = (BitArray)Data.Clone();
        if (divider != 4 && divider != 8)
            throw new ArgumentException("Invalid divider. Can be (4 | 8)", nameof(divider));

        int l = bits.Length;
        int newL = (l + divider - 1) / divider * divider;
        if (l == newL) return this;

        bits.Length = newL;
        bits[bits.Length - (newL - l)] = true;

        return new Bits(bits);
    }

    public Bits Rollback(int divider = 8)
    {
        if (divider != 4 && divider != 8)
            throw new ArgumentException("Invalid divider. Can be (4 | 8)", nameof(divider));

        if (Length < divider) throw new Exception("Bits length is less than divider");

        int? pos = null;

        for (int i = Length - 1; i >= Length - 1 - divider; i--)
            if (Data[i])
            {
                pos = i;
                break;
            }

        if (pos == null) throw new Exception("Incorrectly augmented bits.");

        return Slice(0, (int)pos);
    }

    static BitArray FromString(string s)
    {
        static BitArray fromBinaryString(string bitString)
        {
            BitArray bits = new(bitString.Length);
            for (int i = 0; i < bitString.Length; i++) bits.Set(i, bitString[i] == '1');

            return bits;
        }

        static BitArray fromHexString(string hexStringOrig)
        {
            static BitArray parse(string h)
            {
                BitArray bits = new(h.Length * 4);
                for (int i = 0; i < h.Length; i++)
                {
                    byte b = Convert.ToByte(h.Substring(i, 1), 16);
                    for (int j = 0; j < 4; j++) bits.Set(i * 4 + j, (b & (1 << (3 - j))) != 0);
                }

                return bits;
            }

            string hexString = hexStringOrig;
            bool partialEnd = hexString[hexString.Length - 1] == '_';

            if (!partialEnd) return parse(hexString);

            hexString = hexString.Substring(0, hexString.Length - 1);
            BitArray bits = parse(hexString);

            int lastTrueIndex = -1;
            for (int i = bits.Length - 1; i >= 0; i--)
                if (bits[i])
                {
                    lastTrueIndex = i;
                    break;
                }

            bits.Length = lastTrueIndex;
            return bits;
        }

        static BitArray fromFiftBinary(string fiftBits)
        {
            return fromBinaryString(fiftBits.Substring(2, fiftBits.Length - 3));
        }

        static BitArray fromFiftHex(string fiftHex)
        {
            return fromHexString(fiftHex.Substring(2, fiftHex.Length - 3));
        }

        static BitArray fromBase64(string base64, bool url = false)
        {
            if (url) base64 = base64.Replace('-', '+').Replace('_', '/');

            while (base64.Length % 4 != 0) base64 += "=";

            byte[] bytes = Convert.FromBase64String(base64);
            for (int i = 0; i < bytes.Length; i++) bytes[i] = bytes[i].ReverseBits();

            return new BitArray(bytes);
        }

        BitArray bits;
        if (s.IsBinaryString())
            bits = fromBinaryString(s);
        else if (s.IsHexString())
            bits = fromHexString(s);
        else if (s.IsBase64())
            bits = fromBase64(s);
        else if (s.IsBase64Url())
            bits = fromBase64(s, true);
        else if (s.IsFiftBinary())
            bits = fromFiftBinary(s);
        else if (s.IsFiftHex())
            bits = fromFiftHex(s);
        else
            throw new ArgumentException("Unknown string type, supported: binary, hex, fiftBinary, fiftHex");

        return bits;
    }

    public Bits Hash()
    {
        using (SHA256 algorithm = SHA256.Create())
        {
            byte[] hashBytes = algorithm.ComputeHash(ToBytes());
            return new Bits(hashBytes);
        }
    }

    public T[] GetCopyTo<T>(T[] to)
    {
        Data.CopyTo(to, 0);
        return to;
    }

    public byte[] ToBytes(bool needReverse = true)
    {
        byte[] bytes = GetCopyTo(new byte[(Data.Length + 7) / 8]);

        if (!BitConverter.IsLittleEndian || !needReverse) return bytes;

        for (int i = 0; i < bytes.Length; i++) bytes[i] = bytes[i].ReverseBits();

        return bytes;
    }

    public override string ToString()
    {
        return ToString("base64");
    }

    public string ToString(string mode)
    {
        string toBinaryString(bool fift = false)
        {
            bool[] bools = new bool[Data.Length];
            Data.CopyTo(bools, 0);
            char[] chars = Array.ConvertAll(bools, b => b ? '1' : '0');
            string newStr = new(chars);
            return fift ? $"b{{{newStr}}}" : newStr;
        }

        string toHexString(bool fift = false)
        {
            Bits bits = new(Data);
            int length = Data.Length;
            bool areDivisible = length % 4 == 0;
            BitArray augmented = areDivisible ? bits.Data : bits.Augment(4).Data;
            int charCount = augmented.Length / 4;
            char[] hexChars = new char[charCount + (areDivisible ? 0 : 1)];
            for (int i = 0; i < charCount; i++)
            {
                int value = 0;
                for (int j = 0; j < 4; j++)
                {
                    int index = i * 4 + j;
                    bool bit = index <= length && augmented.Get(index);
                    if (bit) value |= 1 << (3 - j);
                }

                hexChars[i] = HexSymbols[value];
            }

            if (!areDivisible) hexChars[hexChars.Length - 1] = '_';

            string newStr = new(hexChars);
            return fift ? $"x{{{newStr}}}" : newStr;
        }

        string toBase64(bool url = false)
        {
            byte[] bytes = ToBytes();
            string base64 = Convert.ToBase64String(bytes);
            return url
                ? base64.TrimEnd('=').Replace('+', '-').Replace('/', '_')
                : base64;
        }

        return mode switch
        {
            "bin" => toBinaryString(),
            "hex" => toHexString(),
            "fiftBin" => toBinaryString(true),
            "fiftHex" => toHexString(true),
            "base64" => toBase64(),
            "base64url" => toBase64(true),
            _ => throw new ArgumentException(
                "Unknown mode, supported: bin, hex, fiftBin, fiftHex, base64, base64url")
        };
    }

    public virtual Bits Clone()
    {
        return new Bits(Data);
    }

    public BitsSlice Parse()
    {
        return new BitsSlice(this);
    }

    public BitArray Unwrap()
    {
        return Data;
    }

    public override bool Equals(object? obj)
    {
        if (obj == null || GetType() != obj.GetType())
            return false;

        Bits other = (Bits)obj;
        if (Data.Length != other.Data.Length) return false;

        for (int i = 0; i < Data.Length; i++)
            if (Data[i] != other.Data[i])
                return false;

        return true;
    }

    public override int GetHashCode()
    {
        int hash = 17;
        for (int i = 0; i < Data.Length; i++) hash = hash * 31 + Data[i].GetHashCode();

        return hash;
    }
}