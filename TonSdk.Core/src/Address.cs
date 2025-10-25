using System;
using System.Linq;
using System.Text.RegularExpressions;
using TonSdk.Core.Block;
using TonSdk.Core.Boc;
using TonSdk.Core.Crypto;

namespace TonSdk.Core;

/// <summary>
///     Represents a TON blockchain address.
///     Immutable value type consisting of a workchain ID and 32-byte hash.
/// </summary>
public readonly struct Address : IEquatable<Address>
{
    const byte FlagBounceable = 0x11;
    const byte FlagNonBounceable = 0x51;
    const byte FlagTestBounceable = 0x91;
    const byte FlagTestNonBounceable = 0xd1;
    const int HashSize = 32;

    public readonly int Workchain;
    public readonly byte[] Hash;

    public Address(int workchain, byte[] hash)
    {
        Workchain = workchain;
        Hash = hash;
    }

    /// <summary>
    ///     Creates an address from workchain and hash.
    /// </summary>
    public static Address Create(int workchain, byte[] hash)
    {
        if (workchain < -128 || workchain >= 128)
            throw new ArgumentException("Workchain must be int8 (-128 to 127)", nameof(workchain));

        if (hash == null || hash.Length != HashSize)
            throw new ArgumentException($"Hash must be exactly {HashSize} bytes", nameof(hash));

        byte[] hashCopy = new byte[HashSize];
        Array.Copy(hash, hashCopy, HashSize);
        return new Address(workchain, hashCopy);
    }

    /// <summary>
    ///     Creates an address from a StateInit.
    /// </summary>
    public static Address Create(int workchain, StateInit stateInit)
    {
        if (workchain < -128 || workchain >= 128)
            throw new ArgumentException("Workchain must be int8 (-128 to 127)", nameof(workchain));

        byte[] hash = stateInit.Cell.Hash.Parse().LoadBytes(HashSize);
        return new Address(workchain, hash);
    }

    /// <summary>
    ///     Parses an address from string (supports raw format "0:abc..." or base64).
    /// </summary>
    public static Address Parse(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
            throw new ArgumentException("Address string cannot be null or empty", nameof(address));

        if (IsRaw(address))
        {
            (int workchain, byte[] hash) = ParseRaw(address);
            return new Address(workchain, hash);
        }

        if (IsBase64(address))
        {
            (int workchain, byte[] hash, bool bounceable, bool testOnly) = ParseBase64(address);
            return new Address(workchain, hash);
        }

        throw new ArgumentException($"Invalid address format: {address}", nameof(address));
    }

    /// <summary>
    ///     Returns raw format: "0:abc123..."
    /// </summary>
    public string ToRaw()
    {
        return $"{Workchain}:{Utils.BytesToHex(Hash).ToLower()}";
    }

    /// <summary>
    ///     Returns base64-encoded user-friendly format.
    /// </summary>
    public string ToBase64(bool bounceable = true, bool testOnly = false, bool urlSafe = true)
    {
        byte tag = EncodeTag(bounceable, testOnly);

        BitsBuilder addressBits = new BitsBuilder(8 + 8 + 256 + 16)
            .StoreUInt(tag, 8)
            .StoreInt(Workchain, 8)
            .StoreBytes(Hash);

        ushort checksum = Utils.Crc16(addressBits.Data.ToBytes());
        addressBits.StoreUInt(checksum, 16);

        return addressBits.Build().ToString(urlSafe ? "base64url" : "base64");
    }

    /// <summary>
    ///     Returns BOC representation.
    /// </summary>
    public string ToBoc()
    {
        return new CellBuilder().StoreAddress(this).Build().Serialize().ToString();
    }

    /// <summary>
    ///     Default string representation (base64 url-safe bounceable).
    /// </summary>
    public override string ToString()
    {
        return ToBase64();
    }

    #region Parsing

    static bool IsRaw(string address)
    {
        return Regex.IsMatch(address, @"^-?[0-9]:[a-fA-F0-9]{64}$");
    }

    static bool IsBase64(string address)
    {
        return address.Length == 48 && (address.IsBase64() || address.IsBase64Url());
    }

    static (int workchain, byte[] hash) ParseRaw(string value)
    {
        string[] parts = value.Split(':');
        int workchain = int.Parse(parts[0]);
        byte[] hash = new Bits(parts[1]).Parse().LoadBytes(HashSize);
        return (workchain, hash);
    }

    static (int workchain, byte[] hash, bool bounceable, bool testOnly) ParseBase64(string value)
    {
        BitsSlice slice = new Bits(value).Parse();
        byte[] crcBytes = slice.ReadBits(16 + 256).ToBytes();

        byte tag = (byte)slice.LoadUInt(8);
        sbyte workchain = (sbyte)slice.LoadInt(8);
        byte[] hash = slice.LoadBytes(32);
        byte[] checksum = slice.LoadBits(16).ToBytes();
        byte[] crc = Utils.Crc16BytesBigEndian(crcBytes);

        if (!crc.SequenceEqual(checksum))
            throw new Exception("Invalid address checksum");

        (bool bounceable, bool testOnly) = DecodeTag(tag);

        return (workchain, hash, bounceable, testOnly);
    }

    static byte EncodeTag(bool bounceable, bool testOnly)
    {
        if (bounceable && !testOnly) return FlagBounceable;
        if (!bounceable && !testOnly) return FlagNonBounceable;
        if (bounceable && testOnly) return FlagTestBounceable;
        return FlagTestNonBounceable;
    }

    static (bool bounceable, bool testOnly) DecodeTag(byte tag)
    {
        return tag switch
        {
            FlagBounceable => (true, false),
            FlagNonBounceable => (false, false),
            FlagTestBounceable => (true, true),
            FlagTestNonBounceable => (false, true),
            _ => throw new Exception($"Invalid address tag: 0x{tag:X2}")
        };
    }

    #endregion

    #region Equality

    public bool Equals(Address other)
    {
        if (Workchain != other.Workchain)
            return false;

        if (Hash == null && other.Hash == null)
            return true;

        if (Hash == null || other.Hash == null)
            return false;

        return Hash.SequenceEqual(other.Hash);
    }

    public override bool Equals(object? obj)
    {
        return obj is Address other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hashCode = Workchain;
            if (Hash != null)
            {
                foreach (byte b in Hash)
                    hashCode = (hashCode * 31) ^ b;
            }
            return hashCode;
        }
    }

    public static bool operator ==(Address left, Address right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Address left, Address right)
    {
        return !left.Equals(right);
    }

    #endregion
}

/// <summary>
///     Represents an external address (outside TON blockchain).
/// </summary>
public class ExternalAddress(int len, Bits value)
{
    public static bool IsAddress(object src)
    {
        return src is ExternalAddress;
    }

    public override string ToString()
    {
        return $"External<{len}:{value.ToString("base64")}>";
    }
}