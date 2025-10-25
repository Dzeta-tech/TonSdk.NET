using TonSdk.Adnl.TL;

namespace TonSdk.Adnl.LiteClient.Protocol
{
    /// <summary>
    /// Decodes responses from the lite server protocol
    /// Uses auto-generated schema types from Schema.Generated.cs
    /// </summary>
    internal static class Decoder
    {
        public static LiteServerMasterchainInfo DecodeMasterchainInfo(byte[] data)
        {
            var reader = new TLReadBuffer(data);
            return LiteServerMasterchainInfo.ReadFrom(reader);
        }

        public static LiteServerMasterchainInfoExt DecodeMasterchainInfoExt(byte[] data)
        {
            var reader = new TLReadBuffer(data);
            return LiteServerMasterchainInfoExt.ReadFrom(reader);
        }

        public static LiteServerCurrentTime DecodeTime(byte[] data)
        {
            var reader = new TLReadBuffer(data);
            return LiteServerCurrentTime.ReadFrom(reader);
        }

        public static LiteServerVersion DecodeVersion(byte[] data)
        {
            var reader = new TLReadBuffer(data);
            return LiteServerVersion.ReadFrom(reader);
        }

        public static LiteServerBlockData DecodeBlock(byte[] data)
        {
            var reader = new TLReadBuffer(data);
            return LiteServerBlockData.ReadFrom(reader);
        }

        public static LiteServerBlockHeader DecodeBlockHeader(byte[] data)
        {
            var reader = new TLReadBuffer(data);
            return LiteServerBlockHeader.ReadFrom(reader);
        }

        public static LiteServerAllShardsInfo DecodeAllShardsInfo(byte[] data)
        {
            var reader = new TLReadBuffer(data);
            return LiteServerAllShardsInfo.ReadFrom(reader);
        }

        public static LiteServerBlockTransactions DecodeBlockTransactions(byte[] data)
        {
            var reader = new TLReadBuffer(data);
            return LiteServerBlockTransactions.ReadFrom(reader);
        }

        public static LiteServerAccountState DecodeAccountState(byte[] data)
        {
            var reader = new TLReadBuffer(data);
            return LiteServerAccountState.ReadFrom(reader);
        }

        public static LiteServerTransactionList DecodeTransactions(byte[] data)
        {
            var reader = new TLReadBuffer(data);
            return LiteServerTransactionList.ReadFrom(reader);
        }

        public static LiteServerTransactionInfo DecodeTransactionInfo(byte[] data)
        {
            var reader = new TLReadBuffer(data);
            return LiteServerTransactionInfo.ReadFrom(reader);
        }

        public static LiteServerConfigInfo DecodeConfigInfo(byte[] data)
        {
            var reader = new TLReadBuffer(data);
            return LiteServerConfigInfo.ReadFrom(reader);
        }
    }
}

