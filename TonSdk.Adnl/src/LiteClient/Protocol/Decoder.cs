using TonSdk.Adnl.TL;

namespace TonSdk.Adnl.LiteClient.Protocol;

/// <summary>
///     Decodes responses from the lite server protocol
///     Uses auto-generated schema types from Schema.Generated.cs
/// </summary>
internal static class Decoder
{
    public static LiteServerMasterchainInfo DecodeMasterchainInfo(byte[] data)
    {
        TLReadBuffer reader = new(data);
        return LiteServerMasterchainInfo.ReadFrom(reader);
    }

    public static LiteServerMasterchainInfoExt DecodeMasterchainInfoExt(byte[] data)
    {
        TLReadBuffer reader = new(data);
        return LiteServerMasterchainInfoExt.ReadFrom(reader);
    }

    public static LiteServerCurrentTime DecodeTime(byte[] data)
    {
        TLReadBuffer reader = new(data);
        return LiteServerCurrentTime.ReadFrom(reader);
    }

    public static LiteServerVersion DecodeVersion(byte[] data)
    {
        TLReadBuffer reader = new(data);
        return LiteServerVersion.ReadFrom(reader);
    }

    public static LiteServerBlockData DecodeBlock(byte[] data)
    {
        TLReadBuffer reader = new(data);
        return LiteServerBlockData.ReadFrom(reader);
    }

    public static LiteServerBlockHeader DecodeBlockHeader(byte[] data)
    {
        TLReadBuffer reader = new(data);
        return LiteServerBlockHeader.ReadFrom(reader);
    }

    public static LiteServerAllShardsInfo DecodeAllShardsInfo(byte[] data)
    {
        TLReadBuffer reader = new(data);
        return LiteServerAllShardsInfo.ReadFrom(reader);
    }

    public static LiteServerBlockTransactions DecodeBlockTransactions(byte[] data)
    {
        TLReadBuffer reader = new(data);
        return LiteServerBlockTransactions.ReadFrom(reader);
    }

    public static LiteServerAccountState DecodeAccountState(byte[] data)
    {
        TLReadBuffer reader = new(data);
        return LiteServerAccountState.ReadFrom(reader);
    }

    public static LiteServerTransactionList DecodeTransactions(byte[] data)
    {
        TLReadBuffer reader = new(data);
        return LiteServerTransactionList.ReadFrom(reader);
    }

    public static LiteServerTransactionInfo DecodeTransactionInfo(byte[] data)
    {
        TLReadBuffer reader = new(data);
        return LiteServerTransactionInfo.ReadFrom(reader);
    }

    public static LiteServerConfigInfo DecodeConfigInfo(byte[] data)
    {
        TLReadBuffer reader = new(data);
        return LiteServerConfigInfo.ReadFrom(reader);
    }
}