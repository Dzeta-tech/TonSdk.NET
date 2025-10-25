using System;
using System.Numerics;
using TonSdk.Adnl.TL;

namespace TonSdk.Adnl.LiteClient.Protocol;

/// <summary>
///     Encodes requests for the lite server protocol
///     Matches the API from ton-lite-client (TypeScript)
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
        TLWriteBuffer liteQueryWriter = new();
        liteQueryWriter.WriteUInt32(LiteServerQuery);
        liteQueryWriter.WriteBuffer(methodWriter.Build());

        // Wrap in adnl.message.query
        TLWriteBuffer writer = new();
        writer.WriteUInt32(AdnlMessageQuery);
        writer.WriteInt256(new BigInteger(queryId));
        writer.WriteBuffer(liteQueryWriter.Build());

        return (queryId, writer.Build());
    }

    public static byte[] EncodePing()
    {
        TLWriteBuffer writer = new();
        writer.WriteUInt32(TcpPing);

        Random random = new();
        long randomId = ((long)random.Next() << 32) | (uint)random.Next();
        writer.WriteInt64(randomId);

        return writer.Build();
    }

    public static (byte[], byte[]) EncodeMasterchainInfo()
    {
        TLWriteBuffer writer = new();
        writer.WriteUInt32(Functions.GetMasterchainInfo);
        return EncodeRequest(writer);
    }

    public static (byte[], byte[]) EncodeMasterchainInfoExt(uint mode = 0)
    {
        TLWriteBuffer writer = new();
        writer.WriteUInt32(Functions.GetMasterchainInfoExt);
        writer.WriteUInt32(mode);
        return EncodeRequest(writer);
    }

    public static (byte[], byte[]) EncodeTime()
    {
        TLWriteBuffer writer = new();
        writer.WriteUInt32(Functions.GetTime);
        return EncodeRequest(writer);
    }

    public static (byte[], byte[]) EncodeVersion()
    {
        TLWriteBuffer writer = new();
        writer.WriteUInt32(Functions.GetVersion);
        return EncodeRequest(writer);
    }

    public static (byte[], byte[]) EncodeBlock(TonNodeBlockIdExt id)
    {
        TLWriteBuffer writer = new();
        writer.WriteUInt32(Functions.GetBlock);
        id.WriteTo(writer);
        return EncodeRequest(writer);
    }

    public static (byte[], byte[]) EncodeBlockHeader(TonNodeBlockIdExt id, uint mode = 0)
    {
        TLWriteBuffer writer = new();
        writer.WriteUInt32(Functions.GetBlockHeader);
        id.WriteTo(writer);
        writer.WriteUInt32(mode);
        return EncodeRequest(writer);
    }

    public static (byte[], byte[]) EncodeAllShardsInfo(TonNodeBlockIdExt id)
    {
        TLWriteBuffer writer = new();
        writer.WriteUInt32(Functions.GetAllShardsInfo);
        id.WriteTo(writer);
        return EncodeRequest(writer);
    }

    public static (byte[], byte[]) EncodeLookupBlock(TonNodeBlockId id, uint mode, long? lt, uint? utime)
    {
        TLWriteBuffer writer = new();
        writer.WriteUInt32(Functions.LookupBlock);
        writer.WriteUInt32(mode);
        id.WriteTo(writer);

        if ((mode & 2) != 0 && lt.HasValue) writer.WriteInt64(lt.Value);
        if ((mode & 4) != 0 && utime.HasValue) writer.WriteUInt32(utime.Value);

        return EncodeRequest(writer);
    }

    public static (byte[], byte[]) EncodeListBlockTransactions(
        TonNodeBlockIdExt id,
        uint count,
        uint mode,
        LiteServerTransactionId3 after)
    {
        TLWriteBuffer writer = new();
        writer.WriteUInt32(Functions.ListBlockTransactions);
        id.WriteTo(writer);
        writer.WriteUInt32(mode);
        writer.WriteUInt32(count);

        if ((mode & 128) != 0 && after != null) after.WriteTo(writer);

        return EncodeRequest(writer);
    }

    public static (byte[], byte[]) EncodeAccountState(TonNodeBlockIdExt id, LiteServerAccountId account)
    {
        TLWriteBuffer writer = new();
        writer.WriteUInt32(Functions.GetAccountState);
        id.WriteTo(writer);
        account.WriteTo(writer);
        return EncodeRequest(writer);
    }

    public static (byte[], byte[]) EncodeTransactions(uint count, LiteServerAccountId account, long lt, byte[] hash)
    {
        TLWriteBuffer writer = new();
        writer.WriteUInt32(Functions.GetTransactions);
        writer.WriteUInt32(count);
        account.WriteTo(writer);
        writer.WriteInt64(lt);
        writer.WriteBytes(hash, 32);
        return EncodeRequest(writer);
    }

    public static (byte[], byte[]) EncodeSendMessage(byte[] body)
    {
        TLWriteBuffer writer = new();
        writer.WriteUInt32(Functions.SendMessage);
        writer.WriteBuffer(body);
        return EncodeRequest(writer);
    }
}