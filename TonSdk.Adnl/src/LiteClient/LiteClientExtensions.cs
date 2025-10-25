using System.Threading;
using System.Threading.Tasks;
using TonSdk.Adnl.LiteClient.Protocol;
using TonSdk.Adnl.LiteClient.Types;
using TonSdk.Core.Addresses;

namespace TonSdk.Adnl.LiteClient;

/// <summary>
///     Extension methods for LiteClient providing user-friendly APIs
///     that wrap raw TL schema types with parsed/deserialized values.
/// </summary>
public static class LiteClientExtensions
{
    /// <summary>
    ///     List transactions in a block with deserialized account addresses.
    ///     Returns user-friendly BlockTransactionsList instead of raw TL schema type.
    ///     The workchain is automatically extracted from the block ID.
    /// </summary>
    public static async Task<BlockTransactionsList> ListBlockTransactionsParsed(
        this LiteClient client,
        TonNodeBlockIdExt blockId,
        uint count = 1024,
        LiteServerTransactionId3 after = null,
        bool reverseOrder = false,
        bool wantProof = false,
        CancellationToken cancellationToken = default)
    {
        LiteServerBlockTransactions raw = await client.ListBlockTransactions(
            blockId,
            count,
            after,
            reverseOrder,
            wantProof,
            cancellationToken);

        // BlockTransactionsList.FromRaw automatically uses workchain from block ID
        return BlockTransactionsList.FromRaw(raw);
    }

    /// <summary>
    ///     Get account state with parsed address and balance.
    ///     Returns user-friendly AccountState instead of raw TL schema type.
    /// </summary>
    public static async Task<AccountState> GetAccountStateParsed(
        this LiteClient client,
        Address address,
        TonNodeBlockIdExt blockId,
        CancellationToken cancellationToken = default)
    {
        LiteServerAccountId accountId = new()
        {
            Workchain = address.Workchain,
            Id = address.Hash
        };

        LiteServerAccountState raw = await client.GetAccountState(
            blockId,
            accountId,
            cancellationToken);

        return AccountState.FromRaw(raw, address);
    }

    /// <summary>
    ///     Create LiteServerAccountId from Address
    /// </summary>
    public static LiteServerAccountId ToAccountId(this Address address)
    {
        return new LiteServerAccountId
        {
            Workchain = address.Workchain,
            Id = address.Hash
        };
    }
}

