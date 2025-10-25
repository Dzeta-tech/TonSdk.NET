using System;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TonSdk.Core;

namespace TonSdk.Core.EntityFrameworkCore
{
    /// <summary>
    /// EF Core Value Converter for Address struct.
    /// Stores Address as 36 bytes: 4 bytes (int workchain) + 32 bytes (hash)
    /// 
    /// Usage in DbContext.OnModelCreating:
    /// <code>
    /// modelBuilder.Entity&lt;YourEntity&gt;()
    ///     .Property(e => e.WalletAddress)
    ///     .HasConversion(new AddressValueConverter())
    ///     .HasMaxLength(36);
    /// </code>
    /// 
    /// Or use the extension method:
    /// <code>
    /// modelBuilder.Entity&lt;YourEntity&gt;()
    ///     .Property(e => e.WalletAddress)
    ///     .HasAddressConversion();
    /// </code>
    /// </summary>
    public class AddressValueConverter : ValueConverter<Address, byte[]>
    {
        public AddressValueConverter() 
            : base(
                address => ToBytes(address),
                bytes => FromBytes(bytes))
        {
        }

        private static byte[] ToBytes(Address address)
        {
            var bytes = new byte[36];
            
            // Store workchain as 4 bytes (int32)
            BitConverter.GetBytes(address.Workchain).CopyTo(bytes, 0);
            
            // Store hash (32 bytes)
            address.Hash.CopyTo(bytes.AsSpan(4));
            
            return bytes;
        }

        private static Address FromBytes(byte[] bytes)
        {
            if (bytes == null || bytes.Length != 36)
                throw new ArgumentException("Address must be exactly 36 bytes (4 bytes workchain + 32 bytes hash)", nameof(bytes));

            // Read workchain from first 4 bytes
            int workchain = BitConverter.ToInt32(bytes, 0);
            
            // Read hash from remaining 32 bytes
            var hash = new byte[32];
            Array.Copy(bytes, 4, hash, 0, 32);
            
            return new Address(workchain, hash);
        }
    }

    /// <summary>
    /// EF Core Value Converter that stores Address as raw string format "0:abc123..."
    /// More readable in database but takes 70-80 bytes.
    /// 
    /// Usage in DbContext.OnModelCreating:
    /// <code>
    /// modelBuilder.Entity&lt;YourEntity&gt;()
    ///     .Property(e => e.WalletAddress)
    ///     .HasConversion(new AddressStringValueConverter())
    ///     .HasMaxLength(80);
    /// </code>
    /// 
    /// Or use the extension method:
    /// <code>
    /// modelBuilder.Entity&lt;YourEntity&gt;()
    ///     .Property(e => e.WalletAddress)
    ///     .HasAddressStringConversion();
    /// </code>
    /// </summary>
    public class AddressStringValueConverter : ValueConverter<Address, string>
    {
        public AddressStringValueConverter() 
            : base(
                address => address.ToRaw(),
                str => new Address(str))
        {
        }
    }
}

