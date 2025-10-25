# Dzeta.TonSdk.NET

> **Note**: This is a fork of [TonSdk.NET](https://github.com/continuation-team/TonSdk.NET) with critical bug fixes and improvements.
>
> **Key changes**:
> - **Thread-safety fixes**: Replaced `Dictionary` with `ConcurrentDictionary` in LiteClient for concurrent request handling
> - **Bounds checking**: Added BOC deserialization safety checks to prevent "Index out of range" errors
> - **Address struct**: Rewritten `Address` as a high-performance `struct` using `fixed byte` and `ReadOnlySpan<byte>`
> - **EF Core integration**: New extension package for Entity Framework Core value converters
> - **Atomic operations**: Proper use of `TryAdd`/`TryRemove` for thread-safe request management
>
> **Status**: Currently only Core, Adnl, and EntityFrameworkCore packages are published. Other packages (Client, Contracts, Connect, DeFi) will be added incrementally with improvements.

## Packages

### [Dzeta.TonSdk.Core](https://www.nuget.org/packages/Dzeta.TonSdk.Core/)
[![NuGet](https://img.shields.io/nuget/dt/Dzeta.TonSdk.Core.svg)](https://www.nuget.org/packages/Dzeta.TonSdk.Core)
[![NuGet](https://img.shields.io/nuget/vpre/Dzeta.TonSdk.Core.svg)](https://www.nuget.org/packages/Dzeta.TonSdk.Core)

Core library with types and structures for TON Blockchain. Includes:
- `Address` as high-performance `struct` with `fixed byte` hash storage
- `Coins` for TON, Jetton amounts
- BOC serialization/deserialization with bounds checking
- Cell, Builder, Slice
- Mnemonic (BIP39 and TON standards)
- Ed25519 signing

```bash
dotnet add package Dzeta.TonSdk.Core
```

### [Dzeta.TonSdk.Adnl](https://www.nuget.org/packages/Dzeta.TonSdk.Adnl/)
[![NuGet](https://img.shields.io/nuget/dt/Dzeta.TonSdk.Adnl.svg)](https://www.nuget.org/packages/Dzeta.TonSdk.Adnl)
[![NuGet](https://img.shields.io/nuget/vpre/Dzeta.TonSdk.Adnl.svg)](https://www.nuget.org/packages/Dzeta.TonSdk.Adnl)

Thread-safe ADNL client and LiteClient for interacting with TON blockchain nodes. Includes:
- ADNL protocol implementation
- LiteClient with concurrent request handling (`ConcurrentDictionary`)
- Atomic operations for safe multi-threaded access
- Block and account state queries

```bash
dotnet add package Dzeta.TonSdk.Adnl
```

### [Dzeta.TonSdk.Core.EntityFrameworkCore](https://www.nuget.org/packages/Dzeta.TonSdk.Core.EntityFrameworkCore/)
[![NuGet](https://img.shields.io/nuget/dt/Dzeta.TonSdk.Core.EntityFrameworkCore.svg)](https://www.nuget.org/packages/Dzeta.TonSdk.Core.EntityFrameworkCore)
[![NuGet](https://img.shields.io/nuget/vpre/Dzeta.TonSdk.Core.EntityFrameworkCore.svg)](https://www.nuget.org/packages/Dzeta.TonSdk.Core.EntityFrameworkCore)

Entity Framework Core integration for TonSdk.Core types. Includes:
- `AddressValueConverter` - stores Address as 36 bytes (4 bytes workchain + 32 bytes hash)
- `AddressStringValueConverter` - stores Address as human-readable string (~80 bytes)
- Extension methods for easy configuration

```bash
dotnet add package Dzeta.TonSdk.Core.EntityFrameworkCore
```

**Usage example:**
```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Wallet>()
        .Property(e => e.Address)
        .HasAddressConversion<Wallet>(); // Binary storage (36 bytes)
}
```

## Features and Status

- [x] Builder, Cell, Slice
- [x] BOC (de)serialization with bounds checking
- [x] Hashmap(E) (dictionary) (de)serialization
- [x] Mnemonic BIP39 standard
- [x] Mnemonic TON standard
- [x] Coins (class for TON, JETTON, etc.)
- [x] Address (struct with `fixed byte` for high performance)
- [x] Message layouts (MessageX, etc.)
- [x] Popular structures from block.tlb
- [x] Ed25519 signing of transactions
- [x] ADNL client (thread-safe)
- [x] Lite client over ADNL client (thread-safe)
- [x] EF Core value converters for TON types
- [ ] Client with HTTP API (coming soon)
- [ ] Contracts abstraction (coming soon)
- [ ] TON Connect 2.0 (coming soon)
- [ ] DeFi integrations (coming soon)

## Installation

Install via NuGet Package Manager or .NET CLI:

```bash
dotnet add package Dzeta.TonSdk.Core
dotnet add package Dzeta.TonSdk.Adnl
dotnet add package Dzeta.TonSdk.Core.EntityFrameworkCore  # Optional, for EF Core users
```

## Quick Start

### Using Address

```csharp
using TonSdk.Core;

// Parse address
var address = new Address("EQDk2VTvn04SUKJrW7rXahzdF8_Qi6utb0wj43InCu9vdjrR");

// Get raw format
string raw = address.ToRaw(); // "0:e4d954ef9f4e1250a26b5bbad76a1cdd17cfc08babaddf4c23e37227..."

// Get workchain and hash
int workchain = address.Workchain; // 0
ReadOnlySpan<byte> hash = address.Hash; // 32 bytes
```

### Using LiteClient

```csharp
using TonSdk.Adnl;
using TonSdk.Adnl.LiteClient;

var client = new LiteClient(LiteClientOptions.GetFromUrl("https://ton-blockchain.github.io/global.config.json"));
await client.Connect();

var masterchain = await client.GetMasterchainInfo();
Console.WriteLine($"Last block: {masterchain.Last.Seqno}");
```

### Using with Entity Framework Core

```csharp
using TonSdk.Core;
using TonSdk.Core.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public class Wallet
{
    public int Id { get; set; }
    public Address Address { get; set; } // TON address stored efficiently
    public decimal Balance { get; set; }
}

public class MyDbContext : DbContext
{
    public DbSet<Wallet> Wallets { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Binary storage (36 bytes) - recommended
        modelBuilder.Entity<Wallet>()
            .Property(e => e.Address)
            .HasAddressConversion<Wallet>();
    }
}
```

## License

MIT License
