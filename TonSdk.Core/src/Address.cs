using System;
using System.Linq;
using System.Text.RegularExpressions;
using TonSdk.Core.Block;
using TonSdk.Core.Boc;
using TonSdk.Core.Crypto;

namespace TonSdk.Core
{
    /// <summary>
    ///     Represents a TON blockchain address.
    ///     Immutable value type consisting of a workchain ID and 32-byte hash.
    /// </summary>
    public unsafe struct Address : IEquatable<Address>
    {
        const byte FLAG_BOUNCEABLE = 0x11;
        const byte FLAG_NON_BOUNCEABLE = 0x51;
        const byte FLAG_TEST_BOUNCEABLE = 0x91;
        const byte FLAG_TEST_NON_BOUNCEABLE = 0xd1;
        const int HASH_SIZE = 32;

        fixed byte _hash[HASH_SIZE];

        public readonly int Workchain;

        public ReadOnlySpan<byte> Hash
        {
            get
            {
                fixed (byte* ptr = _hash)
                {
                    return new ReadOnlySpan<byte>(ptr, HASH_SIZE);
                }
            }
        }

        /// <summary>
        ///     Creates an address from workchain and hash.
        /// </summary>
        public Address(int workchain, byte[] hash)
        {
            if (workchain < -128 || workchain >= 128)
                throw new ArgumentException("Workchain must be int8 (-128 to 127)", nameof(workchain));

            if (hash == null || hash.Length != HASH_SIZE)
                throw new ArgumentException($"Hash must be exactly {HASH_SIZE} bytes", nameof(hash));

            Workchain = workchain;
            for (int i = 0; i < HASH_SIZE; i++)
                _hash[i] = hash[i];
        }

        /// <summary>
        ///     Creates an address from a StateInit.
        /// </summary>
        public Address(int workchain, StateInit stateInit)
        {
            if (workchain < -128 || workchain >= 128)
                throw new ArgumentException("Workchain must be int8 (-128 to 127)", nameof(workchain));

            Workchain = workchain;
            byte[] hashBytes = stateInit.Cell.Hash.Parse().LoadBytes(HASH_SIZE);
            for (int i = 0; i < HASH_SIZE; i++)
                _hash[i] = hashBytes[i];
        }

        /// <summary>
        ///     Parses an address from string (supports raw format "0:abc..." or base64).
        /// </summary>
        public Address(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentException("Address string cannot be null or empty", nameof(address));

            byte[] hashBytes;
            if (IsRaw(address))
            {
                (int workchain, byte[] hash) parsed = ParseRaw(address);
                Workchain = parsed.workchain;
                hashBytes = parsed.hash;
            }
            else if (IsBase64(address))
            {
                (int workchain, byte[] hash, bool bounceable, bool testOnly) parsed = ParseBase64(address);
                Workchain = parsed.workchain;
                hashBytes = parsed.hash;
            }
            else
            {
                throw new ArgumentException($"Invalid address format: {address}", nameof(address));
            }

            for (int i = 0; i < HASH_SIZE; i++)
                _hash[i] = hashBytes[i];
        }

        /// <summary>
        ///     Returns raw format: "0:abc123..."
        /// </summary>
        public string ToRaw()
        {
            fixed (byte* ptr = _hash)
            {
                ReadOnlySpan<byte> hashBytes = new ReadOnlySpan<byte>(ptr, HASH_SIZE);
                return $"{Workchain}:{Utils.BytesToHex(hashBytes.ToArray()).ToLower()}";
            }
        }

        /// <summary>
        ///     Returns base64-encoded user-friendly format.
        /// </summary>
        public string ToBase64(bool bounceable = true, bool testOnly = false, bool urlSafe = true)
        {
            byte tag = EncodeTag(bounceable, testOnly);

            fixed (byte* ptr = _hash)
            {
                ReadOnlySpan<byte> hashBytes = new ReadOnlySpan<byte>(ptr, HASH_SIZE);
                BitsBuilder addressBits = new BitsBuilder(8 + 8 + 256 + 16)
                    .StoreUInt(tag, 8)
                    .StoreInt(Workchain, 8)
                    .StoreBytes(hashBytes.ToArray());

                ushort checksum = Utils.Crc16(addressBits.Data.ToBytes());
                addressBits.StoreUInt(checksum, 16);

                return addressBits.Build().ToString(urlSafe ? "base64url" : "base64");
            }
        }

        /// <summary>
        ///     Returns BOC representation.
        /// </summary>
        public string ToBOC()
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
            return address.Length == 48 && (address.isBase64() || address.isBase64url());
        }

        static (int workchain, byte[] hash) ParseRaw(string value)
        {
            string[] parts = value.Split(':');
            int workchain = int.Parse(parts[0]);
            byte[] hash = new Bits(parts[1]).Parse().LoadBytes(HASH_SIZE);
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
            if (bounceable && !testOnly) return FLAG_BOUNCEABLE;
            if (!bounceable && !testOnly) return FLAG_NON_BOUNCEABLE;
            if (bounceable && testOnly) return FLAG_TEST_BOUNCEABLE;
            return FLAG_TEST_NON_BOUNCEABLE;
        }

        static (bool bounceable, bool testOnly) DecodeTag(byte tag)
        {
            return tag switch
            {
                FLAG_BOUNCEABLE => (true, false),
                FLAG_NON_BOUNCEABLE => (false, false),
                FLAG_TEST_BOUNCEABLE => (true, true),
                FLAG_TEST_NON_BOUNCEABLE => (false, true),
                _ => throw new Exception($"Invalid address tag: 0x{tag:X2}")
            };
        }

        #endregion

        #region Equality

        public bool Equals(Address other)
        {
            if (Workchain != other.Workchain)
                return false;

            byte* otherPtr = other._hash;
            for (int i = 0; i < HASH_SIZE; i++)
                if (_hash[i] != otherPtr[i])
                    return false;

            return true;
        }

        public override bool Equals(object obj)
        {
            return obj is Address other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = Workchain;
                for (int i = 0; i < HASH_SIZE; i++)
                    hash = (hash * 31) ^ _hash[i];
                return hash;
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
    public class ExternalAddress
    {
        readonly int _len;
        readonly Bits _value;

        public ExternalAddress(int len, Bits value)
        {
            _len = len;
            _value = value;
        }

        public static bool IsAddress(object src)
        {
            return src is ExternalAddress;
        }

        public override string ToString()
        {
            return $"External<{_len}:{_value.ToString("base64")}>";
        }
    }
}