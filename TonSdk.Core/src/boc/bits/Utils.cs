using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TonSdk.Core.Boc;

public static class BitsPatterns
{
    const string BinaryString = @"^[01]+$";
    const string HexString = @"^([0-9a-fA-F]+|[0-9a-fA-F]*[1-9a-fA-F]+_?)$";
    const string FiftBinary = @"^b\{[01]+\}$";
    const string FiftHex = @"^x\{([0-9a-fA-F]+|[0-9a-fA-F]*[1-9a-fA-F]+_?)\}$";
    const string Base64 = @"^(?:[A-Za-z0-9+\/]{4})*(?:[A-Za-z0-9+\/]{2}==|[A-Za-z0-9+\/]{3}=)?$";
    const string Base64Url = @"^(?:[A-Za-z0-9-_]{4})*(?:[A-Za-z0-9-_]{2}==|[A-Za-z0-9-_]{3}=)?$";

    static readonly Regex BinaryStringRegex = new(BinaryString);
    static readonly Regex HexStringRegex = new(HexString);
    static readonly Regex FiftBinaryRegex = new(FiftBinary);
    static readonly Regex FiftHexRegex = new(FiftHex);
    static readonly Regex Base64Regex = new(Base64);
    static readonly Regex Base64UrlRegex = new(Base64Url);

    public static bool IsBinaryString(this string s)
    {
        return BinaryStringRegex.IsMatch(s);
    }

    public static bool IsHexString(this string s)
    {
        return HexStringRegex.IsMatch(s);
    }

    public static bool IsFiftBinary(this string s)
    {
        return FiftBinaryRegex.IsMatch(s);
    }

    public static bool IsFiftHex(this string s)
    {
        return FiftHexRegex.IsMatch(s);
    }

    public static bool IsBase64(this string s)
    {
        return Base64Regex.IsMatch(s);
    }

    public static bool IsBase64Url(this string s)
    {
        return Base64UrlRegex.IsMatch(s);
    }
}

public class BitsEqualityComparer : IEqualityComparer<Bits>
{
    public bool Equals(Bits x, Bits y)
    {
        if (x.Length != y.Length)
            return false;
        for (int i = 0; i < x.Length; i++)
            if (x.Data[i] != y.Data[i])
                return false;

        return true;
    }

    public int GetHashCode(Bits obj)
    {
        Bits bits = obj.Hash();
        return bits.GetCopyTo(new int[8])[0];
    }
}

public static class BitArrayUtils
{
    public static BitArray Slice(this BitArray bits, int start, int end, bool inplace = false)
    {
        if (start < 0 || end < 0 || start > end || end > bits.Length)
            throw new ArgumentException($"Invalid slice indexes: {start}, {end}");

        BitArray data = inplace ? bits : (BitArray)bits.Clone();
        BitArray ret = new(end - start);
        for (int i = start; i < end; i++) ret[i - start] = data[i];
        return ret;
    }
}