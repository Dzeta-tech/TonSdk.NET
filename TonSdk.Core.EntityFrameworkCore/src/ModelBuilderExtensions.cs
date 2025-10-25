using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TonSdk.Core;

namespace TonSdk.Core.EntityFrameworkCore
{
    /// <summary>
    /// Extension methods for configuring TON types in Entity Framework Core models.
    /// </summary>
    public static class ModelBuilderExtensions
    {
        /// <summary>
        /// Configures an Address property to be stored as 36 bytes (4 bytes workchain + 32 bytes hash).
        /// </summary>
        /// <typeparam name="TEntity">The entity type</typeparam>
        /// <param name="propertyBuilder">The property builder</param>
        /// <returns>The property builder for chaining</returns>
        public static PropertyBuilder<Address> HasAddressConversion<TEntity>(
            this PropertyBuilder<Address> propertyBuilder) where TEntity : class
        {
            return propertyBuilder
                .HasConversion(new AddressValueConverter())
                .HasMaxLength(36);
        }

        /// <summary>
        /// Configures an Address property to be stored as a raw string "0:abc123..." (70-80 bytes).
        /// More readable in database but takes more space.
        /// </summary>
        /// <typeparam name="TEntity">The entity type</typeparam>
        /// <param name="propertyBuilder">The property builder</param>
        /// <returns>The property builder for chaining</returns>
        public static PropertyBuilder<Address> HasAddressStringConversion<TEntity>(
            this PropertyBuilder<Address> propertyBuilder) where TEntity : class
        {
            return propertyBuilder
                .HasConversion(new AddressStringValueConverter())
                .HasMaxLength(80);
        }

        /// <summary>
        /// Configures an Address property to be stored as 36 bytes (4 bytes workchain + 32 bytes hash).
        /// Nullable version.
        /// </summary>
        /// <typeparam name="TEntity">The entity type</typeparam>
        /// <param name="propertyBuilder">The property builder</param>
        /// <returns>The property builder for chaining</returns>
        public static PropertyBuilder<Address?> HasAddressConversion<TEntity>(
            this PropertyBuilder<Address?> propertyBuilder) where TEntity : class
        {
            return propertyBuilder
                .HasConversion(new AddressValueConverter())
                .HasMaxLength(36);
        }

        /// <summary>
        /// Configures an Address property to be stored as a raw string "0:abc123..." (70-80 bytes).
        /// More readable in database but takes more space. Nullable version.
        /// </summary>
        /// <typeparam name="TEntity">The entity type</typeparam>
        /// <param name="propertyBuilder">The property builder</param>
        /// <returns>The property builder for chaining</returns>
        public static PropertyBuilder<Address?> HasAddressStringConversion<TEntity>(
            this PropertyBuilder<Address?> propertyBuilder) where TEntity : class
        {
            return propertyBuilder
                .HasConversion(new AddressStringValueConverter())
                .HasMaxLength(80);
        }
    }
}

