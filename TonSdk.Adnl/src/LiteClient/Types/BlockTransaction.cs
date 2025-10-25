using System;
using TonSdk.Adnl.LiteClient.Protocol;
using TonSdk.Core.Addresses;

namespace TonSdk.Adnl.LiteClient.Types;

/// <summary>
///     User-friendly representation of a block transaction.
///     Wraps the raw TL schema type with parsed values.
/// </summary>
public readonly struct BlockTransaction(Address account, long lt, byte[] hash)
{
    public readonly Address Account = account;
    public readonly long Lt = lt;
    public readonly byte[] Hash = hash;

    /// <summary>
    ///     Parse from raw TL schema type
    /// </summary>
    public static BlockTransaction FromRaw(LiteServerTransactionId raw, int defaultWorkchain = 0)
    {
        // In LiteServerTransactionId, Account is stored as raw 32 bytes (just the hash part)
        // The workchain information is not included in this structure
        // We default to workchain 0 (mainnet) unless specified otherwise

        if (raw.Account is not { Length: 32 })
            throw new ArgumentException("Invalid account data");

        Address account = Address.Create(defaultWorkchain, raw.Account);

        return new BlockTransaction(account, raw.Lt, raw.Hash);
    }

    /// <summary>
    ///     Parse from LiteServerAccountId (which includes workchain)
    /// </summary>
    public static Address AddressFromAccountId(LiteServerAccountId accountId)
    {
        if (accountId.Id is not { Length: 32 })
            throw new ArgumentException("Invalid account ID");

        return Address.Create(accountId.Workchain, accountId.Id);
    }
}

/// <summary>
///     User-friendly representation of block transactions list.
/// </summary>
public class BlockTransactionsList(
    TonNodeBlockIdExt blockId,
    BlockTransaction[] transactions,
    bool incomplete,
    byte[] proof)
{
    public readonly TonNodeBlockIdExt BlockId = blockId;
    public readonly bool Incomplete = incomplete;
    public readonly byte[] Proof = proof;
    public readonly BlockTransaction[] Transactions = transactions;

    /// <summary>
    ///     Parse from raw TL schema type.
    ///     Uses the workchain from the block ID for all transaction addresses.
    /// </summary>
    public static BlockTransactionsList FromRaw(LiteServerBlockTransactions raw)
    {
        // Use workchain from block ID since all transactions in a block are in the same workchain
        int workchain = raw.Id.Workchain;

        BlockTransaction[] transactions = new BlockTransaction[raw.Ids.Length];
        for (int i = 0; i < raw.Ids.Length; i++) transactions[i] = BlockTransaction.FromRaw(raw.Ids[i], workchain);

        return new BlockTransactionsList(
            raw.Id,
            transactions,
            raw.Incomplete,
            raw.Proof
        );
    }
}