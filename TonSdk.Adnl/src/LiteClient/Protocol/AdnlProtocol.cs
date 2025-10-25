using System;
using System.Numerics;
using TonSdk.Adnl.TL;

namespace TonSdk.Adnl.LiteClient.Protocol;

/// <summary>
///     Handles ADNL protocol message wrapping and unwrapping.
///     Encapsulates the protocol layer between lite client and ADNL transport.
/// </summary>
internal static class AdnlProtocol
{
    const uint AdnlMessageQuery = 0x6A118B44; // adnl.message.query
    const uint AdnlMessageAnswer = 0xB4E874E4; // adnl.message.answer
    const uint TcpPong = 0x0A9276D4; // tcp.pong
    const uint LiteServerQuery = 0x7AF98BB4; // liteServer.query

    /// <summary>
    ///     Wrap a lite server query in ADNL protocol layers.
    ///     Returns (queryId, wrappedPacket).
    /// </summary>
    public static (byte[] queryId, byte[] packet) WrapQuery(byte[] liteServerQuery)
    {
        byte[] queryId = AdnlKeys.GenerateRandomBytes(32);

        // Wrap in liteServer.query
        TLWriteBuffer liteQueryWriter = new();
        liteQueryWriter.WriteUInt32(LiteServerQuery);
        liteQueryWriter.WriteBuffer(liteServerQuery);

        // Wrap in adnl.message.query
        TLWriteBuffer adnlWriter = new();
        adnlWriter.WriteUInt32(AdnlMessageQuery);
        adnlWriter.WriteInt256(new BigInteger(queryId));
        adnlWriter.WriteBuffer(liteQueryWriter.Build());

        return (queryId, adnlWriter.Build());
    }

    /// <summary>
    ///     Unwrap ADNL response and extract query ID and lite server response.
    ///     Returns (queryId, liteServerResponse) or null if it's a pong message.
    /// </summary>
    public static (byte[] queryId, byte[] response)? UnwrapResponse(byte[] data)
    {
        TLReadBuffer reader = new(data);

        // Read ADNL message type
        uint messageType = reader.ReadUInt32();

        // Handle pong messages (heartbeat responses)
        if (messageType == TcpPong)
            return null;

        // Verify it's an answer message
        if (messageType != AdnlMessageAnswer)
            throw new Exception($"Unexpected ADNL message type: 0x{messageType:X8}");

        // Read query ID (32 bytes)
        byte[] queryId = reader.ReadBytes(32);

        // Read lite server response (length-prefixed)
        byte[] liteServerResponse = reader.ReadBuffer();

        return (queryId, liteServerResponse);
    }

    /// <summary>
    ///     Check if response data is a lite server error.
    ///     If it is, throws an exception with the error details.
    ///     Otherwise returns the response data after the constructor.
    /// </summary>
    public static byte[] ValidateAndExtractResponse(byte[] liteServerResponse)
    {
        TLReadBuffer reader = new(liteServerResponse);

        // Read response constructor
        uint responseCode = reader.ReadUInt32();

        // Check for liteServer.error
        if (responseCode == LiteServerError.Constructor)
        {
            LiteServerError? error = LiteServerError.ReadFrom(reader);
            throw new Exception($"LiteServer error {error.Code}: {error.Message}");
        }

        // Return remaining data (the actual response)
        return reader.ReadObject();
    }
}