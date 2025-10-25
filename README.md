# Dzeta.TonSdk.NET

> **Note**: This is a fork of [TonSdk.NET](https://github.com/continuation-team/TonSdk.NET) with critical bug fixes for thread-safety issues in the LiteClient. The original SDK had race conditions when multiple threads/processes accessed the lite client concurrently, leading to data corruption and "Index out of range" errors during BOC deserialization.
>
> **Key fixes**:
> - Replaced `Dictionary` with `ConcurrentDictionary` for thread-safe request handling
> - Added bounds checking in BOC deserialization for extra safety
> - Uses proper atomic operations for request management

## Packages

### [Dzeta.TonSdk.Core](https://www.nuget.org/packages/Dzeta.TonSdk.Core/)
[![NuGet](https://img.shields.io/nuget/dt/Dzeta.TonSdk.Core.svg)](https://www.nuget.org/packages/Dzeta.TonSdk.Core)
[![NuGet](https://img.shields.io/nuget/vpre/Dzeta.TonSdk.Core.svg)](https://www.nuget.org/packages/Dzeta.TonSdk.Core) \
Core library with types and structures for TON Blockchain

### [Dzeta.TonSdk.Client](https://www.nuget.org/packages/Dzeta.TonSdk.Client/)
[![NuGet](https://img.shields.io/nuget/dt/Dzeta.TonSdk.Client.svg)](https://www.nuget.org/packages/Dzeta.TonSdk.Client)
[![NuGet](https://img.shields.io/nuget/vpre/Dzeta.TonSdk.Client.svg)](https://www.nuget.org/packages/Dzeta.TonSdk.Client) \
RPC Client for work with TonCenter API

### [Dzeta.TonSdk.Contracts](https://www.nuget.org/packages/Dzeta.TonSdk.Contracts/)
[![NuGet](https://img.shields.io/nuget/dt/Dzeta.TonSdk.Contracts.svg)](https://www.nuget.org/packages/Dzeta.TonSdk.Contracts)
[![NuGet](https://img.shields.io/nuget/vpre/Dzeta.TonSdk.Contracts.svg)](https://www.nuget.org/packages/Dzeta.TonSdk.Contracts) \
Abstractions for work with smart contracts in TON Blockchain

### [Dzeta.TonSdk.Connect](https://www.nuget.org/packages/Dzeta.TonSdk.Connect/)
[![NuGet](https://img.shields.io/nuget/dt/Dzeta.TonSdk.Connect.svg)](https://www.nuget.org/packages/Dzeta.TonSdk.Connect)
[![NuGet](https://img.shields.io/nuget/vpre/Dzeta.TonSdk.Connect.svg)](https://www.nuget.org/packages/Dzeta.TonSdk.Connect) \
Library to work with Ton Connect 2.0

### [Dzeta.TonSdk.Adnl](https://www.nuget.org/packages/Dzeta.TonSdk.Adnl/)
[![NuGet](https://img.shields.io/nuget/dt/Dzeta.TonSdk.Adnl.svg)](https://www.nuget.org/packages/Dzeta.TonSdk.Adnl)
[![NuGet](https://img.shields.io/nuget/vpre/Dzeta.TonSdk.Adnl.svg)](https://www.nuget.org/packages/Dzeta.TonSdk.Adnl) \
Library to work with Ton ADNL

### [Dzeta.TonSdk.DeFi](https://www.nuget.org/packages/Dzeta.TonSdk.DeFi/)
[![NuGet](https://img.shields.io/nuget/dt/Dzeta.TonSdk.Defi.svg)](https://www.nuget.org/packages/Dzeta.TonSdk.DeFi)
[![NuGet](https://img.shields.io/nuget/vpre/Dzeta.TonSdk.DeFi.svg)](https://www.nuget.org/packages/Dzeta.TonSdk.DeFi) \
Library to work with Ton DeFi`s

## Features and status

- [x] Builder, Cell, Slice
- [x] BOC  (de)serialization
- [x] Hashmap(E) (dictionary) (de)serialization
- [x] Mnemonic BIP39 standard
- [x] Mnemonic TON standard
- [x] Coins (class for TON, JETTON, e.t.c.)
- [x] Address (class for TON address)
- [x] Message layouts (such as MessageX e.t.c.)
- [x] Popular structures from block.tlb
- [x] Contracts (abstract TON contract class)
- [x] Ed25519 signing of transactions
- [x] ADNL client
- [x] Lite client over ADNL client
- [x] Ton Client with HTTP API and Lite client
- [ ] ~100% tests coverage

## Documentation
[TonSDK.NET Docs](https://docs.tonsdk.net/user-manual/getting-started)

## Support
You can ask questions that may arise during the use of the SDK in our [Telegram group](https://t.me/cont_team/104).

## Donation

`matthewparker.ton`

## License

MIT License
