using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TonSdk.Core;
using TonSdk.Core.Cryptography;

namespace TonSdk.Adnl.LiteClient.Engines;

/// <summary>
///     Logging decorator for engines.
///     Use this to add logging to any engine via DI.
/// </summary>
public class LoggingEngine : LiteEngineDecorator
{
    readonly ILogger<LoggingEngine> logger;

    public LoggingEngine(ILiteEngine innerEngine, ILogger<LoggingEngine> logger) : base(innerEngine)
    {
        this.logger = logger;
    }

    public override async Task<byte[]> QueryAsync(
        Func<(byte[] queryId, byte[] data)> encoder,
        int timeout = 30000,
        CancellationToken cancellationToken = default)
    {
        (byte[] queryId, byte[] data) = encoder();
        string queryIdHex = Utils.BytesToHex(queryId);
        
        logger.LogDebug("Query {QueryId} starting, timeout={Timeout}ms, packetSize={PacketSize}",
            queryIdHex, timeout, data.Length);
        
        Stopwatch sw = Stopwatch.StartNew();
        try
        {
            byte[] result = await base.QueryAsync(() => (queryId, data), timeout, cancellationToken);
            sw.Stop();
            
            logger.LogDebug("Query {QueryId} completed in {ElapsedMs}ms, responseSize={ResponseSize}",
                queryIdHex, sw.ElapsedMilliseconds, result.Length);
            
            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogError(ex, "Query {QueryId} failed after {ElapsedMs}ms", queryIdHex, sw.ElapsedMilliseconds);
            throw;
        }
    }
}

