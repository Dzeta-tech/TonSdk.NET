using System;
using TonSdk.Adnl.LiteClient.Protocol;
using TonSdk.Core.Addresses;
using TonSdk.Core.Blocks;
using TonSdk.Core.Boc;
using TonSdk.Core.Boc.bits;
using TonSdk.Core.Boc.Cells;
using TonSdk.Core.Economics;
using CellSlice = TonSdk.Core.Boc.Cells.CellSlice;

namespace TonSdk.Adnl.LiteClient.Types;

/// <summary>
///     User-friendly representation of account state.
///     Wraps the raw TL schema type with parsed and deserialized values.
/// </summary>
public class ClientAccountState(
    Address address,
    Coins balance,
    AccountStatus status,
    Cell code,
    Cell data,
    long lastTransactionLt,
    byte[] lastTransactionHash,
    byte[] rawState,
    byte[] proof,
    TonNodeBlockIdExt block,
    TonNodeBlockIdExt shardBlock,
    byte[] shardProof)
{
    public readonly Address Address = address;
    public readonly Coins Balance = balance;

    /// <summary>
    ///     Block this state was queried from
    /// </summary>
    public readonly TonNodeBlockIdExt Block = block;

    public readonly Cell Code = code;
    public readonly Cell Data = data;
    public readonly byte[] LastTransactionHash = lastTransactionHash;
    public readonly long LastTransactionLt = lastTransactionLt;

    /// <summary>
    ///     Raw proof data
    /// </summary>
    public readonly byte[] Proof = proof;

    /// <summary>
    ///     Raw account state from blockchain (BOC format)
    /// </summary>
    public readonly byte[] RawState = rawState;

    /// <summary>
    ///     Shard block
    /// </summary>
    public readonly TonNodeBlockIdExt ShardBlock = shardBlock;

    /// <summary>
    ///     Shard proof
    /// </summary>
    public readonly byte[] ShardProof = shardProof;

    public readonly AccountStatus Status = status;

    /// <summary>
    ///     Parse from raw TL schema type.
    ///     Deserializes the account state from BOC format following JS SDK implementation.
    /// </summary>
    public static ClientAccountState FromRaw(LiteServerAccountState raw, Address address)
    {
        // Default values for non-existent account
        if (raw.State == null || raw.State.Length == 0)
            return new ClientAccountState(
                address,
                Coins.Zero,
                AccountStatus.Nonexist,
                null,
                null,
                0,
                Array.Empty<byte>(),
                raw.State ?? Array.Empty<byte>(),
                raw.Proof,
                raw.Id,
                raw.Shardblk,
                raw.ShardProof
            );

        try
        {
            // Parse BOC to get Cell (following old SDK: Cell.From(new Bits(accountStateBytes)).Parse())
            Cell[] cells = BagOfCells.DeserializeBoc(new Bits(raw.State));
            if (cells.Length == 0) throw new Exception("Empty BOC");

            CellSlice accountSlice = cells[0].Parse();

            // Check if account exists (first bit)
            if (!accountSlice.LoadBit())
                // account_none$0 = Account
                return new ClientAccountState(
                    address,
                    Coins.Zero,
                    AccountStatus.Nonexist,
                    null,
                    null,
                    0,
                    Array.Empty<byte>(),
                    raw.State,
                    raw.Proof,
                    raw.Id,
                    raw.Shardblk,
                    raw.ShardProof
                );

            // Parse the account (following old SDK)
            Account account = Account.Load(accountSlice);

            // Extract data from parsed account
            Coins balance = account.Storage.Balance;
            AccountStatus status = account.Storage.State.Status;
            Cell code = account.Storage.State.Code;
            Cell data = account.Storage.State.Data;
            long lastTransLt = account.Storage.LastTransLt;

            // Try to get last transaction hash from proof (following JS implementation)
            byte[] lastTransHash = Array.Empty<byte>();
            try
            {
                // The proof contains shard state with account info
                Cell[] proofCells = BagOfCells.DeserializeBoc(new Bits(raw.Proof));
                if (proofCells.Length > 1 && proofCells[1].Refs.Length > 0)
                    // TODO: Parse shard state to get last transaction hash
                    // For now, we'll leave it empty
                    lastTransHash = Array.Empty<byte>();
            }
            catch
            {
                // If proof parsing fails, continue without last hash
            }

            return new ClientAccountState(
                address,
                balance,
                status,
                code,
                data,
                lastTransLt,
                lastTransHash,
                raw.State,
                raw.Proof,
                raw.Id,
                raw.Shardblk,
                raw.ShardProof
            );
        }
        catch (Exception ex)
        {
            // Failed to parse, return minimal info
            return new ClientAccountState(
                address,
                Coins.Zero,
                AccountStatus.Nonexist,
                null,
                null,
                0,
                Array.Empty<byte>(),
                raw.State,
                raw.Proof,
                raw.Id,
                raw.Shardblk,
                raw.ShardProof
            );
        }
    }
}