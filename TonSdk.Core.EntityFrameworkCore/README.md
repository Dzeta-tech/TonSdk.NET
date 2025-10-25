# TonSdk.Core.EntityFrameworkCore

Entity Framework Core integration for TonSdk.Core, providing value converters for TON blockchain types.

## Installation

```bash
dotnet add package Dzeta.TonSdk.Core.EntityFrameworkCore
```

## Features

- **AddressValueConverter**: Stores `Address` as 36 bytes (4 bytes workchain + 32 bytes hash)
- **AddressStringValueConverter**: Stores `Address` as raw string format "0:abc123..." (more readable, ~80 bytes)
- Extension methods for easy configuration

## Usage

### Basic Usage with Converter

```csharp
using TonSdk.Core;
using TonSdk.Core.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public class MyDbContext : DbContext
{
    public DbSet<Wallet> Wallets { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Option 1: Binary storage (36 bytes) - recommended
        modelBuilder.Entity<Wallet>()
            .Property(e => e.Address)
            .HasConversion(new AddressValueConverter())
            .HasMaxLength(36);

        // Option 2: String storage (~80 bytes) - more readable
        modelBuilder.Entity<Wallet>()
            .Property(e => e.Address)
            .HasConversion(new AddressStringValueConverter())
            .HasMaxLength(80);
    }
}

public class Wallet
{
    public int Id { get; set; }
    public Address Address { get; set; }
    public decimal Balance { get; set; }
}
```

### Using Extension Methods (Recommended)

```csharp
using TonSdk.Core;
using TonSdk.Core.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public class MyDbContext : DbContext
{
    public DbSet<Wallet> Wallets { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Binary storage (36 bytes) - recommended
        modelBuilder.Entity<Wallet>()
            .Property(e => e.Address)
            .HasAddressConversion<Wallet>();

        // Or string storage (~80 bytes) - more readable
        modelBuilder.Entity<Wallet>()
            .Property(e => e.Address)
            .HasAddressStringConversion<Wallet>();
    }
}
```

### Working with Nullable Addresses

```csharp
public class Transaction
{
    public int Id { get; set; }
    public Address FromAddress { get; set; }
    public Address? ToAddress { get; set; } // Nullable
}

// In OnModelCreating:
modelBuilder.Entity<Transaction>()
    .Property(e => e.FromAddress)
    .HasAddressConversion<Transaction>();

modelBuilder.Entity<Transaction>()
    .Property(e => e.ToAddress)
    .HasAddressConversion<Transaction>(); // Works with nullable too
```

## Storage Comparison

| Converter | Storage Size | Format | Pros | Cons |
|-----------|--------------|---------|------|------|
| `AddressValueConverter` | 36 bytes | Binary | Compact, efficient | Not human-readable in DB |
| `AddressStringValueConverter` | ~80 bytes | String "0:abc..." | Human-readable | Takes more space |

## Migration Example

```bash
dotnet ef migrations add AddWalletAddress
dotnet ef database update
```

## License

MIT

