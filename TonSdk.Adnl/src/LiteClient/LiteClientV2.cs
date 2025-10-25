using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TonSdk.Core;
using TonSdk.Core.Boc;
using TonSdk.Core.Crypto;

namespace TonSdk.Adnl.LiteClient
{
    /// <summary>
    /// High-level LiteClient that works with any ILiteEngine implementation.
    /// Provides type-safe methods for querying the TON lite server.
    /// Thread-safe when using thread-safe engines (like LiteSingleEngine).
    /// </summary>
    public class LiteClientV2 : IDisposable
    {
        readonly ILiteEngine _engine;
        readonly bool _disposeEngine;

        /// <summary>
        /// Create a LiteClient with the specified engine.
        /// </summary>
        /// <param name="engine">The engine to use for queries</param>
        /// <param name="disposeEngine">Whether to dispose the engine when client is disposed</param>
        public LiteClientV2(ILiteEngine engine, bool disposeEngine = true)
        {
            _engine = engine;
            _disposeEngine = disposeEngine;
        }

        /// <summary>
        /// Create a LiteClient with a single connection engine (most common use case).
        /// </summary>
        public static LiteClientV2 CreateSingle(string host, int port, string publicKey, int reconnectTimeoutMs = 10000, ILogger? logger = null)
        {
            var engine = new LiteSingleEngine(host, port, Utils.HexToBytes(publicKey), reconnectTimeoutMs, logger);
            return new LiteClientV2(engine, disposeEngine: true);
        }

        /// <summary>
        /// Create a LiteClient with a single connection engine (most common use case).
        /// </summary>
        public static LiteClientV2 CreateSingle(string host, int port, byte[] publicKey, int reconnectTimeoutMs = 10000, ILogger? logger = null)
        {
            var engine = new LiteSingleEngine(host, port, publicKey, reconnectTimeoutMs, logger);
            return new LiteClientV2(engine, disposeEngine: true);
        }

        public ILiteEngine Engine => _engine;

        /// <summary>
        /// Get extended masterchain information.
        /// </summary>
        public async Task<MasterChainInfoExtended> GetMasterChainInfoExtendedAsync(
            int timeout = 30000,
            CancellationToken cancellationToken = default)
        {
            byte[] response = await _engine.QueryAsync(
                () => LiteClientEncoder.EncodeGetMasterchainInfoExt(),
                timeout,
                cancellationToken);

            return LiteClientDecoder.DecodeGetMasterchainInfoExtended(new TL.TLReadBuffer(response));
        }

        /// <summary>
        /// Get all shards information for a block.
        /// </summary>
        public async Task<byte[]> GetAllShardsInfoAsync(
            BlockIdExtended blockId,
            int timeout = 30000,
            CancellationToken cancellationToken = default)
        {
            byte[] response = await _engine.QueryAsync(
                () => LiteClientEncoder.EncodeGetAllShardsInfo(blockId),
                timeout,
                cancellationToken);

            return LiteClientDecoder.DecodeGetAllShardsInfo(new TL.TLReadBuffer(response));
        }

        /// <summary>
        /// Look up a block by workchain, shard, and seqno.
        /// </summary>
        public async Task<BlockHeader?> LookupBlockAsync(
            int workchain,
            long shard,
            long? seqno = null,
            long? lt = null,
            int? utime = null,
            int timeout = 30000,
            CancellationToken cancellationToken = default)
        {
            byte[] response = await _engine.QueryAsync(
                () => LiteClientEncoder.EncodeLookUpBlock(workchain, shard, seqno, (ulong?)lt, (ulong?)utime),
                timeout,
                cancellationToken);

            return LiteClientDecoder.DecodeBlockHeader(new TL.TLReadBuffer(response));
        }

        /// <summary>
        /// List transactions in a block.
        /// </summary>
        public async Task<ListBlockTransactionsResult> ListBlockTransactionsAsync(
            BlockIdExtended blockId,
            uint count = 10000,
            uint mode = 7,
            ITransactionId? after = null,
            bool? reverseOrder = null,
            bool? wantProof = null,
            int timeout = 30000,
            CancellationToken cancellationToken = default)
        {
            byte[] response = await _engine.QueryAsync(
                () => LiteClientEncoder.EncodeListBlockTransactions(
                    blockId,
                    count,
                    after,
                    reverseOrder,
                    wantProof,
                    "liteServer.listBlockTransactions id:tonNode.blockIdExt mode:# count:# after:mode.7?liteServer.transactionId3 reverse_order:mode.6?true want_proof:mode.5?true = liteServer.BlockTransactions"),
                timeout,
                cancellationToken);

            return LiteClientDecoder.DecodeListBlockTransactions(new TL.TLReadBuffer(response));
        }

        /// <summary>
        /// Get masterchain information (non-extended).
        /// </summary>
        public async Task<MasterChainInfo> GetMasterChainInfoAsync(
            int timeout = 30000,
            CancellationToken cancellationToken = default)
        {
            byte[] response = await _engine.QueryAsync(
                () => LiteClientEncoder.EncodeGetMasterchainInfo(),
                timeout,
                cancellationToken);

            return LiteClientDecoder.DecodeGetMasterchainInfo(new TL.TLReadBuffer(response));
        }

        /// <summary>
        /// Get current server time.
        /// </summary>
        public async Task<int> GetTimeAsync(
            int timeout = 30000,
            CancellationToken cancellationToken = default)
        {
            byte[] response = await _engine.QueryAsync(
                () => LiteClientEncoder.EncodeGetTime(),
                timeout,
                cancellationToken);

            return LiteClientDecoder.DecodeGetTime(new TL.TLReadBuffer(response));
        }

        /// <summary>
        /// Get server version.
        /// </summary>
        public async Task<ChainVersion> GetVersionAsync(
            int timeout = 30000,
            CancellationToken cancellationToken = default)
        {
            byte[] response = await _engine.QueryAsync(
                () => LiteClientEncoder.EncodeGetVersion(),
                timeout,
                cancellationToken);

            return LiteClientDecoder.DecodeGetVersion(new TL.TLReadBuffer(response));
        }

        public void Dispose()
        {
            if (_disposeEngine)
            {
                _engine?.Dispose();
            }
        }
    }
}

