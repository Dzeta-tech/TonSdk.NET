using System;
using TonSdk.Core.Addresses;

namespace TonSdk.Adnl.LiteClient.Types;

/// <summary>
///     User-friendly representation of a block transaction.
///     Wraps the raw TL schema type with parsed values.
/// </summary>
public readonly struct BlockTransaction
{
    public readonly Address Account;
    public readonly long Lt;
    public readonly byte[] Hash;

    public BlockTransaction(Address account, long lt, byte[] hash)
    {
        Account = account;
        Lt = lt;
        Hash = hash;
    }

    /// <summary>
    ///     Parse from raw TL schema type
    /// </summary>
    public static BlockTransaction FromRaw(Protocol.LiteServerTransactionId raw, int defaultWorkchain = 0)
    {
        // In LiteServerTransactionId, Account is stored as raw 32 bytes (just the hash part)
        // The workchain information is not included in this structure
        // We default to workchain 0 (mainnet) unless specified otherwise
        
        if (raw.Account == null || raw.Account.Length != 32)
            throw new ArgumentException("Invalid account data");
        
        Address account = Address.Create(defaultWorkchain, raw.Account);
        
        return new BlockTransaction(account, raw.Lt, raw.Hash);
    }
    
    /// <summary>
    ///     Parse from LiteServerAccountId (which includes workchain)
    /// </summary>
    public static Address AddressFromAccountId(Protocol.LiteServerAccountId accountId)
    {
        if (accountId.Id == null || accountId.Id.Length != 32)
            throw new ArgumentException("Invalid account ID");
            
        return Address.Create(accountId.Workchain, accountId.Id);
    }
}

/// <summary>
///     User-friendly representation of block transactions list.
/// </summary>
public class BlockTransactionsList
{
    public readonly Protocol.TonNodeBlockIdExt BlockId;
    public readonly BlockTransaction[] Transactions;
    public readonly bool Incomplete;
    public readonly byte[] Proof;

    public BlockTransactionsList(
        Protocol.TonNodeBlockIdExt blockId,
        BlockTransaction[] transactions,
        bool incomplete,
        byte[] proof)
    {
        BlockId = blockId;
        Transactions = transactions;
        Incomplete = incomplete;
        Proof = proof;
    }

    /// <summary>
    ///     Parse from raw TL schema type.
    ///     Uses the workchain from the block ID for all transaction addresses.
    /// </summary>
    public static BlockTransactionsList FromRaw(Protocol.LiteServerBlockTransactions raw)
    {
        // Use workchain from block ID since all transactions in a block are in the same workchain
        int workchain = raw.Id.Workchain;
        
        BlockTransaction[] transactions = new BlockTransaction[raw.Ids.Length];
        for (int i = 0; i < raw.Ids.Length; i++)
        {
            transactions[i] = BlockTransaction.FromRaw(raw.Ids[i], workchain);
        }

        return new BlockTransactionsList(
            raw.Id,
            transactions,
            raw.Incomplete,
            raw.Proof
        );
    }
}

