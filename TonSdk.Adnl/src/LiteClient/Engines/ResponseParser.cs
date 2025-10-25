using TonSdk.Adnl.LiteClient.Protocol;

namespace TonSdk.Adnl.LiteClient.Engines;

/// <summary>
///     Parses raw ADNL responses into lite server data.
///     Handles protocol unwrapping and error checking.
/// </summary>
internal static class ResponseParser
{
    public static (byte[] queryId, byte[] response)? Parse(byte[] data)
    {
        // Unwrap ADNL protocol layers
        (byte[] queryId, byte[] response)? unwrapped = AdnlProtocol.UnwrapResponse(data);
        if (!unwrapped.HasValue)
            return null; // Pong message, ignore

        (byte[] queryId, byte[] liteServerResponse) = unwrapped.Value;

        // Validate and extract actual response (checks for errors)
        byte[] responseData = AdnlProtocol.ValidateAndExtractResponse(liteServerResponse);

        return (queryId, responseData);
    }
}