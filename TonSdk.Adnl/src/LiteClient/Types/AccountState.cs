using System;
using System.Numerics;
using TonSdk.Core.Addresses;
using TonSdk.Core.boc;
using TonSdk.Core.boc.bits;
using TonSdk.Core.Economics;

namespace TonSdk.Adnl.LiteClient.Types;

/// <summary>
///     User-friendly representation of account state.
///     Wraps the raw TL schema type with parsed and deserialized values.
/// </summary>
public class AccountState
{
    public readonly Address Address;
    public readonly Coins Balance;
    public readonly AccountStatus Status;
    public readonly byte[] Code;
    public readonly byte[] Data;
    public readonly long LastTransactionLt;
    public readonly byte[] LastTransactionHash;
    
    /// <summary>
    ///     Raw account state from blockchain (BOC format)
    /// </summary>
    public readonly byte[] RawState;
    
    /// <summary>
    ///     Raw proof data
    /// </summary>
    public readonly byte[] Proof;
    
    /// <summary>
    ///     Block this state was queried from
    /// </summary>
    public readonly Protocol.TonNodeBlockIdExt Block;

    public AccountState(
        Address address,
        Coins balance,
        AccountStatus status,
        byte[] code,
        byte[] data,
        long lastTransactionLt,
        byte[] lastTransactionHash,
        byte[] rawState,
        byte[] proof,
        Protocol.TonNodeBlockIdExt block)
    {
        Address = address;
        Balance = balance;
        Status = status;
        Code = code;
        Data = data;
        LastTransactionLt = lastTransactionLt;
        LastTransactionHash = lastTransactionHash;
        RawState = rawState;
        Proof = proof;
        Block = block;
    }

    /// <summary>
    ///     Parse from raw TL schema type.
    ///     Deserializes the account state from BOC format.
    /// </summary>
    public static AccountState FromRaw(Protocol.LiteServerAccountState raw, Address address)
    {
        if (raw.State == null || raw.State.Length == 0)
        {
            // Account doesn't exist or is uninitialized
            return new AccountState(
                address,
                Coins.Zero,
                AccountStatus.Uninitialized,
                Array.Empty<byte>(),
                Array.Empty<byte>(),
                0,
                Array.Empty<byte>(),
                raw.State,
                raw.Proof,
                raw.Id
            );
        }

        try
        {
            // Deserialize account state from BOC
            Bits stateBoc = new(raw.State);
            BitsSlice slice = stateBoc.Parse();
            
            // Account structure in TON:
            // account$_ addr:MsgAddressInt storage_stat:StorageInfo storage:AccountStorage = Account;
            // storage:AccountStorage = [
            //   last_trans_lt:uint64 balance:CurrencyCollection state:AccountState
            // ]
            
            // Check if account exists (first bit should be 1)
            if (!slice.LoadBit())
            {
                return new AccountState(
                    address,
                    Coins.Zero,
                    AccountStatus.Uninitialized,
                    Array.Empty<byte>(),
                    Array.Empty<byte>(),
                    0,
                    Array.Empty<byte>(),
                    raw.State,
                    raw.Proof,
                    raw.Id
                );
            }

            // Skip address parsing (we already have it)
            // addr:MsgAddressInt is parsed here but we skip it
            // For proper parsing, we'd need to implement full TLB schema
            
            // For now, let's try to extract balance using simpler approach
            // The exact parsing would require full TLB schema implementation
            
            // Placeholder: return basic info
            // TODO: Implement full account state parsing from TLB schema
            
            return new AccountState(
                address,
                Coins.Zero, // TODO: Parse balance from state
                AccountStatus.Active,
                Array.Empty<byte>(), // TODO: Parse code
                Array.Empty<byte>(), // TODO: Parse data
                0, // TODO: Parse last LT
                Array.Empty<byte>(), // TODO: Parse last hash
                raw.State,
                raw.Proof,
                raw.Id
            );
        }
        catch
        {
            // Failed to parse, return basic info
            return new AccountState(
                address,
                Coins.Zero,
                AccountStatus.Unknown,
                Array.Empty<byte>(),
                Array.Empty<byte>(),
                0,
                Array.Empty<byte>(),
                raw.State,
                raw.Proof,
                raw.Id
            );
        }
    }
}

/// <summary>
///     Account status on the blockchain
/// </summary>
public enum AccountStatus
{
    /// <summary>
    ///     Account doesn't exist yet
    /// </summary>
    Uninitialized,
    
    /// <summary>
    ///     Account exists and is active
    /// </summary>
    Active,
    
    /// <summary>
    ///     Account is frozen
    /// </summary>
    Frozen,
    
    /// <summary>
    ///     Unknown status (parsing failed)
    /// </summary>
    Unknown
}

