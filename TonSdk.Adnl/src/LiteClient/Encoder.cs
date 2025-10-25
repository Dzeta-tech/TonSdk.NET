using System;
using System.Numerics;
using TonSdk.Adnl.TL;
using TonSdk.Core;

namespace TonSdk.Adnl.LiteClient
{
    /// <summary>
    /// Encodes requests for the lite server protocol
    /// Matches the API from ton-lite-client (TypeScript)
    /// </summary>
    internal static class Encoder
    {
        const uint AdnlMessageQuery = 0x6A118B44; // adnl.message.query
        const uint LiteServerQuery = 0x7AF98BB4; // liteServer.query
        const uint TcpPing = 0x9A2B084D; // tcp.ping

        static (byte[] queryId, byte[] data) EncodeRequest(TLWriteBuffer methodWriter)
        {
            byte[] queryId = AdnlKeys.GenerateRandomBytes(32);
            
            // Wrap method in liteServer.query
            var liteQueryWriter = new TLWriteBuffer();
            liteQueryWriter.WriteUInt32(LiteServerQuery);
            liteQueryWriter.WriteBuffer(methodWriter.Build());

            // Wrap in adnl.message.query
            var writer = new TLWriteBuffer();
            writer.WriteUInt32(AdnlMessageQuery);
            writer.WriteInt256(new BigInteger(queryId));
            writer.WriteBuffer(liteQueryWriter.Build());

            return (queryId, writer.Build());
        }

        public static byte[] EncodePing()
        {
            var writer = new TLWriteBuffer();
            writer.WriteUInt32(TcpPing);
            
            var random = new Random();
            long randomId = ((long)random.Next() << 32) | (uint)random.Next();
            writer.WriteInt64(randomId);
            
            return writer.Build();
        }

        public static (byte[], byte[]) EncodeMasterchainInfo()
        {
            var writer = new TLWriteBuffer();
            writer.WriteUInt32(Functions.GetMasterchainInfo);
            return EncodeRequest(writer);
        }

        public static (byte[], byte[]) EncodeMasterchainInfoExt(uint mode = 0)
        {
            var writer = new TLWriteBuffer();
            writer.WriteUInt32(Functions.GetMasterchainInfoExt);
            writer.WriteUInt32(mode);
            return EncodeRequest(writer);
        }

        public static (byte[], byte[]) EncodeTime()
        {
            var writer = new TLWriteBuffer();
            writer.WriteUInt32(Functions.GetTime);
            return EncodeRequest(writer);
        }

        public static (byte[], byte[]) EncodeVersion()
        {
            var writer = new TLWriteBuffer();
            writer.WriteUInt32(Functions.GetVersion);
            return EncodeRequest(writer);
        }

        public static (byte[], byte[]) EncodeBlock(BlockIdExt id)
        {
            var writer = new TLWriteBuffer();
            writer.WriteUInt32(Functions.GetBlock);
            id.WriteTo(writer);
            return EncodeRequest(writer);
        }

        public static (byte[], byte[]) EncodeBlockHeader(BlockIdExt id, uint mode = 0)
        {
            var writer = new TLWriteBuffer();
            writer.WriteUInt32(Functions.GetBlockHeader);
            id.WriteTo(writer);
            writer.WriteUInt32(mode);
            return EncodeRequest(writer);
        }

        public static (byte[], byte[]) EncodeAllShardsInfo(BlockIdExt id)
        {
            var writer = new TLWriteBuffer();
            writer.WriteUInt32(Functions.GetAllShardsInfo);
            id.WriteTo(writer);
            return EncodeRequest(writer);
        }

        public static (byte[], byte[]) EncodeLookupBlock(BlockId id, uint mode, long? lt, uint? utime)
        {
            var writer = new TLWriteBuffer();
            writer.WriteUInt32(Functions.LookupBlock);
            writer.WriteUInt32(mode);
            id.WriteTo(writer);
            
            if ((mode & 2) != 0 && lt.HasValue) writer.WriteInt64(lt.Value);
            if ((mode & 4) != 0 && utime.HasValue) writer.WriteUInt32(utime.Value);
            
            return EncodeRequest(writer);
        }

        public static (byte[], byte[]) EncodeListBlockTransactions(
            BlockIdExt id,
            uint count,
            uint mode,
            LiteServerTransactionId3 after)
        {
            var writer = new TLWriteBuffer();
            writer.WriteUInt32(Functions.ListBlockTransactions);
            id.WriteTo(writer);
            writer.WriteUInt32(mode);
            writer.WriteUInt32(count);
            
            if ((mode & 128) != 0 && after != null)
            {
                after.WriteTo(writer);
            }
            
            return EncodeRequest(writer);
        }

        public static (byte[], byte[]) EncodeAccountState(BlockIdExt id, LiteServerAccountId account)
        {
            var writer = new TLWriteBuffer();
            writer.WriteUInt32(Functions.GetAccountState);
            id.WriteTo(writer);
            account.WriteTo(writer);
            return EncodeRequest(writer);
        }

        public static (byte[], byte[]) EncodeTransactions(uint count, LiteServerAccountId account, long lt, byte[] hash)
        {
            var writer = new TLWriteBuffer();
            writer.WriteUInt32(Functions.GetTransactions);
            writer.WriteUInt32(count);
            account.WriteTo(writer);
            writer.WriteInt64(lt);
            writer.WriteBytes(hash, 32);
            return EncodeRequest(writer);
        }

        public static (byte[], byte[]) EncodeSendMessage(byte[] body)
        {
            var writer = new TLWriteBuffer();
            writer.WriteUInt32(Functions.SendMessage);
            writer.WriteBuffer(body);
            return EncodeRequest(writer);
        }
    }
}

