using System;
using System.Linq;
using System.Text.RegularExpressions;
using TonSdk.Core.Block;
using TonSdk.Core.Boc;
using TonSdk.Core.Crypto;

namespace TonSdk.Core
{
    /// <summary>
    /// Represents a TON blockchain address.
    /// Immutable value type consisting of a workchain ID and 32-byte hash.
    /// </summary>
    public unsafe struct Address : IEquatable<Address>
    {
        private const byte FLAG_BOUNCEABLE = 0x11;
        private const byte FLAG_NON_BOUNCEABLE = 0x51;
        private const byte FLAG_TEST_BOUNCEABLE = 0x91;
        private const byte FLAG_TEST_NON_BOUNCEABLE = 0xd1;
        private const int HASH_SIZE = 32;

        private fixed byte _hash[HASH_SIZE];

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
        /// Creates an address from workchain and hash.
        /// </summary>
        public Address(int workchain, byte[] hash)
        {
            if (workchain < -128 || workchain >= 128)
                throw new ArgumentException("Workchain must be int8 (-128 to 127)", nameof(workchain));
            
            if (hash == null || hash.Length != HASH_SIZE)
                throw new ArgumentException($"Hash must be exactly {HASH_SIZE} bytes", nameof(hash));

            Workchain = workchain;
            fixed (byte* ptr = _hash)
            {
                for (int i = 0; i < HASH_SIZE; i++)
                    ptr[i] = hash[i];
            }
        }

        /// <summary>
        /// Creates an address from a StateInit.
        /// </summary>
        public Address(int workchain, StateInit stateInit)
        {
            if (workchain < -128 || workchain >= 128)
                throw new ArgumentException("Workchain must be int8 (-128 to 127)", nameof(workchain));

            Workchain = workchain;
            var hashBytes = stateInit.Cell.Hash.Parse().LoadBytes(HASH_SIZE);
            fixed (byte* ptr = _hash)
            {
                for (int i = 0; i < HASH_SIZE; i++)
                    ptr[i] = hashBytes[i];
            }
        }

        /// <summary>
        /// Parses an address from string (supports raw format "0:abc..." or base64).
        /// </summary>
        public Address(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentException("Address string cannot be null or empty", nameof(address));

            byte[] hashBytes;
            if (IsRaw(address))
            {
                var parsed = ParseRaw(address);
                Workchain = parsed.workchain;
                hashBytes = parsed.hash;
            }
            else if (IsBase64(address))
            {
                var parsed = ParseBase64(address);
                Workchain = parsed.workchain;
                hashBytes = parsed.hash;
            }
            else
            {
                throw new ArgumentException($"Invalid address format: {address}", nameof(address));
            }

            fixed (byte* ptr = _hash)
            {
                for (int i = 0; i < HASH_SIZE; i++)
                    ptr[i] = hashBytes[i];
            }
        }

        /// <summary>
        /// Returns raw format: "0:abc123..."
        /// </summary>
        public string ToRaw()
        {
            fixed (byte* ptr = _hash)
            {
                var hashBytes = new ReadOnlySpan<byte>(ptr, HASH_SIZE);
                return $"{Workchain}:{Utils.BytesToHex(hashBytes.ToArray()).ToLower()}";
            }
        }

        /// <summary>
        /// Returns base64-encoded user-friendly format.
        /// </summary>
        public string ToBase64(bool bounceable = true, bool testOnly = false, bool urlSafe = true)
        {
            byte tag = EncodeTag(bounceable, testOnly);
            
            fixed (byte* ptr = _hash)
            {
                var hashBytes = new ReadOnlySpan<byte>(ptr, HASH_SIZE);
                var addressBits = new BitsBuilder(8 + 8 + 256 + 16)
                    .StoreUInt(tag, 8)
                    .StoreInt(Workchain, 8)
                    .StoreBytes(hashBytes.ToArray());

                var checksum = Crypto.Utils.Crc16(addressBits.Data.ToBytes());
                addressBits.StoreUInt(checksum, 16);

                return addressBits.Build().ToString(urlSafe ? "base64url" : "base64");
            }
        }

        /// <summary>
        /// Returns BOC representation.
        /// </summary>
        public string ToBOC()
        {
            return new CellBuilder().StoreAddress(this).Build().Serialize().ToString();
        }

        /// <summary>
        /// Default string representation (base64 url-safe bounceable).
        /// </summary>
        public override string ToString()
        {
            return ToBase64(bounceable: true, testOnly: false, urlSafe: true);
        }

        #region Parsing

        private static bool IsRaw(string address)
        {
            return Regex.IsMatch(address, @"^-?[0-9]:[a-fA-F0-9]{64}$");
        }

        private static bool IsBase64(string address)
        {
            return address.Length == 48 && (BitsPatterns.isBase64(address) || BitsPatterns.isBase64url(address));
        }

        private static (int workchain, byte[] hash) ParseRaw(string value)
        {
            var parts = value.Split(':');
            var workchain = int.Parse(parts[0]);
            var hash = new Bits(parts[1]).Parse().LoadBytes(HASH_SIZE);
            return (workchain, hash);
        }

        private static (int workchain, byte[] hash, bool bounceable, bool testOnly) ParseBase64(string value)
        {
            BitsSlice slice = new Bits(value).Parse();
            byte[] crcBytes = slice.ReadBits(16 + 256).ToBytes();

            byte tag = (byte)slice.LoadUInt(8);
            sbyte workchain = (sbyte)slice.LoadInt(8);
            byte[] hash = slice.LoadBytes(32);
            byte[] checksum = slice.LoadBits(16).ToBytes();
            byte[] crc = Crypto.Utils.Crc16BytesBigEndian(crcBytes);

            if (!crc.SequenceEqual(checksum))
                throw new Exception("Invalid address checksum");

            var (bounceable, testOnly) = DecodeTag(tag);

            return (workchain, hash, bounceable, testOnly);
        }

        private static byte EncodeTag(bool bounceable, bool testOnly)
        {
            if (bounceable && !testOnly) return FLAG_BOUNCEABLE;
            if (!bounceable && !testOnly) return FLAG_NON_BOUNCEABLE;
            if (bounceable && testOnly) return FLAG_TEST_BOUNCEABLE;
            return FLAG_TEST_NON_BOUNCEABLE;
        }

        private static (bool bounceable, bool testOnly) DecodeTag(byte tag)
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

            fixed (byte* thisPtr = _hash)
            {
                byte* otherPtr = other._hash;
                for (int i = 0; i < HASH_SIZE; i++)
                {
                    if (thisPtr[i] != otherPtr[i])
                        return false;
                }
            }
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
                fixed (byte* ptr = _hash)
                {
                    for (int i = 0; i < HASH_SIZE; i++)
                        hash = (hash * 31) ^ ptr[i];
                }
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
    /// Represents an external address (outside TON blockchain).
    /// </summary>
    public class ExternalAddress
    {
        private readonly int _len;
        private readonly Bits _value;

        public ExternalAddress(int len, Bits value)
        {
            _len = len;
            _value = value;
        }

        public static bool IsAddress(object src) => src is ExternalAddress;

        public override string ToString() => $"External<{_len}:{_value.ToString("base64")}>";
    }
}
