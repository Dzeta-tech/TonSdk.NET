using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TonSdk.Adnl.LiteClient.Engines;
using TonSdk.Adnl.LiteClient.Protocol;
using TonSdk.Core;
using TonSdk.Core.Crypto;

namespace TonSdk.Adnl.LiteClient
{
    /// <summary>
    /// High-level lite client for TON blockchain
    /// Matches the API from ton-lite-client (TypeScript)
    /// </summary>
    public class LiteClient : IDisposable
    {
        readonly ILiteEngine _engine;
        readonly bool _disposeEngine;

        public ILiteEngine Engine => _engine;

        LiteClient(ILiteEngine engine, bool disposeEngine = true)
        {
            _engine = engine;
            _disposeEngine = disposeEngine;
        }

        /// <summary>
        /// Create a lite client with a single TCP connection
        /// </summary>
        public static LiteClient Create(
            string host,
            int port,
            byte[] publicKey,
            int reconnectTimeoutMs = 10000,
            ILogger? logger = null)
        {
            var engine = new LiteSingleEngine(host, port, publicKey, reconnectTimeoutMs, logger);
            return new LiteClient(engine, disposeEngine: true);
        }

        /// <summary>
        /// Create a lite client with a single TCP connection
        /// </summary>
        public static LiteClient Create(
            string host,
            int port,
            string publicKeyHex,
            int reconnectTimeoutMs = 10000,
            ILogger? logger = null)
        {
            return Create(host, port, Utils.HexToBytes(publicKeyHex), reconnectTimeoutMs, logger);
        }

        /// <summary>
        /// Create a lite client with a custom engine
        /// </summary>
        public static LiteClient Create(ILiteEngine engine)
        {
            return new LiteClient(engine, disposeEngine: false);
        }

        /// <summary>
        /// Get masterchain info
        /// </summary>
        public async Task<LiteServerMasterchainInfo> GetMasterchainInfo(
            CancellationToken cancellationToken = default)
        {
            byte[] response = await _engine.QueryAsync(
                Encoder.EncodeMasterchainInfo,
                cancellationToken: cancellationToken);

            return Decoder.DecodeMasterchainInfo(response);
        }

        /// <summary>
        /// Get extended masterchain info
        /// </summary>
        public async Task<LiteServerMasterchainInfoExt> GetMasterchainInfoExt(
            uint mode = 0,
            CancellationToken cancellationToken = default)
        {
            byte[] response = await _engine.QueryAsync(
                () => Encoder.EncodeMasterchainInfoExt(mode),
                cancellationToken: cancellationToken);

            return Decoder.DecodeMasterchainInfoExt(response);
        }

        /// <summary>
        /// Get current time from the lite server
        /// </summary>
        public async Task<LiteServerCurrentTime> GetTime(
            CancellationToken cancellationToken = default)
        {
            byte[] response = await _engine.QueryAsync(
                Encoder.EncodeTime,
                cancellationToken: cancellationToken);

            return Decoder.DecodeTime(response);
        }

        /// <summary>
        /// Get lite server version
        /// </summary>
        public async Task<LiteServerVersion> GetVersion(
            CancellationToken cancellationToken = default)
        {
            byte[] response = await _engine.QueryAsync(
                Encoder.EncodeVersion,
                cancellationToken: cancellationToken);

            return Decoder.DecodeVersion(response);
        }

        /// <summary>
        /// Get block data
        /// </summary>
        public async Task<LiteServerBlockData> GetBlock(
            TonNodeBlockIdExt id,
            CancellationToken cancellationToken = default)
        {
            byte[] response = await _engine.QueryAsync(
                () => Encoder.EncodeBlock(id),
                cancellationToken: cancellationToken);

            return Decoder.DecodeBlock(response);
        }

        /// <summary>
        /// Get block header
        /// </summary>
        public async Task<LiteServerBlockHeader> GetBlockHeader(
            TonNodeBlockIdExt id,
            uint mode = 0,
            CancellationToken cancellationToken = default)
        {
            byte[] response = await _engine.QueryAsync(
                () => Encoder.EncodeBlockHeader(id, mode),
                cancellationToken: cancellationToken);

            return Decoder.DecodeBlockHeader(response);
        }

        /// <summary>
        /// Get all shards info for a given masterchain block
        /// </summary>
        public async Task<LiteServerAllShardsInfo> GetAllShardsInfo(
            TonNodeBlockIdExt id,
            CancellationToken cancellationToken = default)
        {
            byte[] response = await _engine.QueryAsync(
                () => Encoder.EncodeAllShardsInfo(id),
                cancellationToken: cancellationToken);

            return Decoder.DecodeAllShardsInfo(response);
        }

        /// <summary>
        /// Lookup block by workchain, shard, and optional parameters
        /// </summary>
        public async Task<LiteServerBlockHeader> LookupBlock(
            int workchain,
            long shard,
            int? seqno = null,
            long? lt = null,
            uint? utime = null,
            CancellationToken cancellationToken = default)
        {
            uint mode = 0;
            if (seqno.HasValue) mode |= 1;
            if (lt.HasValue) mode |= 2;
            if (utime.HasValue) mode |= 4;

            var blockId = new TonNodeBlockId(workchain, shard, seqno ?? 0);

            byte[] response = await _engine.QueryAsync(
                () => Encoder.EncodeLookupBlock(blockId, mode, lt, utime),
                cancellationToken: cancellationToken);

            return Decoder.DecodeBlockHeader(response);
        }

        /// <summary>
        /// List transactions in a block
        /// </summary>
        public async Task<LiteServerBlockTransactions> ListBlockTransactions(
            TonNodeBlockIdExt id,
            uint count = 1024,
            LiteServerTransactionId3 after = null,
            bool reverseOrder = false,
            bool wantProof = false,
            CancellationToken cancellationToken = default)
        {
            uint mode = 0;
            if (wantProof) mode |= 32;      // bit 5
            if (reverseOrder) mode |= 64;    // bit 6
            if (after != null) mode |= 128; // bit 7

            byte[] response = await _engine.QueryAsync(
                () => Encoder.EncodeListBlockTransactions(id, count, mode, after),
                cancellationToken: cancellationToken);

            return Decoder.DecodeBlockTransactions(response);
        }

        /// <summary>
        /// Get account state
        /// </summary>
        public async Task<LiteServerAccountState> GetAccountState(
            TonNodeBlockIdExt id,
            LiteServerAccountId account,
            CancellationToken cancellationToken = default)
        {
            byte[] response = await _engine.QueryAsync(
                () => Encoder.EncodeAccountState(id, account),
                cancellationToken: cancellationToken);

            return Decoder.DecodeAccountState(response);
        }

        /// <summary>
        /// Get account state by address
        /// </summary>
        public async Task<LiteServerAccountState> GetAccountState(
            TonNodeBlockIdExt id,
            Address address,
            CancellationToken cancellationToken = default)
        {
            var accountId = new LiteServerAccountId 
            { 
                Workchain = address.Workchain, 
                Id = address.Hash.ToArray() 
            };
            return await GetAccountState(id, accountId, cancellationToken);
        }

        /// <summary>
        /// Get transactions for an account
        /// </summary>
        public async Task<LiteServerTransactionList> GetTransactions(
            uint count,
            LiteServerAccountId account,
            long lt,
            byte[] hash,
            CancellationToken cancellationToken = default)
        {
            byte[] response = await _engine.QueryAsync(
                () => Encoder.EncodeTransactions(count, account, lt, hash),
                cancellationToken: cancellationToken);

            return Decoder.DecodeTransactions(response);
        }

        /// <summary>
        /// Send a message to the network
        /// </summary>
        public async Task<byte[]> SendMessage(
            byte[] body,
            CancellationToken cancellationToken = default)
        {
            return await _engine.QueryAsync(
                () => Encoder.EncodeSendMessage(body),
                cancellationToken: cancellationToken);
        }

        public void Dispose()
        {
            if (_disposeEngine)
            {
                _engine.Dispose();
            }
        }
    }
}
