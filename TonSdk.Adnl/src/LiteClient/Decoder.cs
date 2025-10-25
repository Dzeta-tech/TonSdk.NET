using TonSdk.Adnl.TL;

namespace TonSdk.Adnl.LiteClient
{
    /// <summary>
    /// Decodes responses from the lite server protocol
    /// Uses auto-generated schema types from Schema.Generated.cs
    /// </summary>
    internal static class Decoder
    {
        public static MasterchainInfo DecodeMasterchainInfo(byte[] data)
        {
            var reader = new TLReadBuffer(data);
            return MasterchainInfo.ReadFrom(reader);
        }

        public static MasterchainInfoExt DecodeMasterchainInfoExt(byte[] data)
        {
            var reader = new TLReadBuffer(data);
            return MasterchainInfoExt.ReadFrom(reader);
        }

        public static CurrentTime DecodeTime(byte[] data)
        {
            var reader = new TLReadBuffer(data);
            return CurrentTime.ReadFrom(reader);
        }

        public static LiteServerVersion DecodeVersion(byte[] data)
        {
            var reader = new TLReadBuffer(data);
            return LiteServerVersion.ReadFrom(reader);
        }

        public static BlockData DecodeBlock(byte[] data)
        {
            var reader = new TLReadBuffer(data);
            return BlockData.ReadFrom(reader);
        }

        public static BlockHeader DecodeBlockHeader(byte[] data)
        {
            var reader = new TLReadBuffer(data);
            return BlockHeader.ReadFrom(reader);
        }

        public static AllShardsInfo DecodeAllShardsInfo(byte[] data)
        {
            var reader = new TLReadBuffer(data);
            return AllShardsInfo.ReadFrom(reader);
        }

        public static BlockTransactions DecodeBlockTransactions(byte[] data)
        {
            var reader = new TLReadBuffer(data);
            return BlockTransactions.ReadFrom(reader);
        }

        public static AccountState DecodeAccountState(byte[] data)
        {
            var reader = new TLReadBuffer(data);
            return AccountState.ReadFrom(reader);
        }

        public static TransactionList DecodeTransactions(byte[] data)
        {
            var reader = new TLReadBuffer(data);
            return TransactionList.ReadFrom(reader);
        }

        public static TransactionInfo DecodeTransactionInfo(byte[] data)
        {
            var reader = new TLReadBuffer(data);
            return TransactionInfo.ReadFrom(reader);
        }

        public static ConfigInfo DecodeConfigInfo(byte[] data)
        {
            var reader = new TLReadBuffer(data);
            return ConfigInfo.ReadFrom(reader);
        }
    }
}

