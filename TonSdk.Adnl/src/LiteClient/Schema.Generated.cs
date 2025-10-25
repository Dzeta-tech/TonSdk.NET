// Auto-generated from lite_api.tl
// DO NOT EDIT MANUALLY
// Union types: Bool, adnl.Message, liteServer.BlockLink

using System;
using TonSdk.Adnl.TL;
using TonSdk.Core;

namespace TonSdk.Adnl.LiteClient
{
    // ============================================================================
    // Abstract base classes for union types
    // ============================================================================

    /// <summary>
    /// Base class for Bool
    /// Implementations: BoolTrue, BoolFalse
    /// </summary>
    public abstract class Bool
    {
        public abstract uint Constructor { get; }
        public abstract void WriteTo(TLWriteBuffer writer);
    }

    /// <summary>
    /// Base class for adnl.Message
    /// Implementations: AdnlMessageQuery, AdnlMessageAnswer
    /// </summary>
    public abstract class AdnlMessage
    {
        public abstract uint Constructor { get; }
        public abstract void WriteTo(TLWriteBuffer writer);
    }

    /// <summary>
    /// Base class for liteServer.BlockLink
    /// Implementations: BlockLinkBack, BlockLinkForward
    /// </summary>
    public abstract class BlockLink
    {
        public abstract uint Constructor { get; }
        public abstract void WriteTo(TLWriteBuffer writer);
    }

    // ============================================================================
    // Basic Types (tonNode.*)
    // ============================================================================

    /// <summary>
    /// tonNode.blockId = tonNode.BlockId
    /// </summary>
    public readonly struct BlockId
    {
        public readonly int Workchain;
        public readonly long Shard;
        public readonly int Seqno;

        public BlockId(int workchain, long shard, int seqno)
        {
            Workchain = workchain;
            Shard = shard;
            Seqno = seqno;
        }

        public  void WriteTo(TLWriteBuffer writer)
        {
            writer.WriteInt32(Workchain);
            writer.WriteInt64(Shard);
            writer.WriteInt32(Seqno);
        }

        public static BlockId ReadFrom(TLReadBuffer reader)
        {
            return new BlockId(
                reader.ReadInt32(),
                reader.ReadInt64(),
                reader.ReadInt32()
            );
        }
    }

    /// <summary>
    /// tonNode.blockIdExt = tonNode.BlockIdExt
    /// </summary>
    public readonly struct BlockIdExt
    {
        public readonly int Workchain;
        public readonly long Shard;
        public readonly int Seqno;
        public readonly byte[] RootHash;
        public readonly byte[] FileHash;

        public BlockIdExt(int workchain, long shard, int seqno, byte[] rootHash, byte[] fileHash)
        {
            Workchain = workchain;
            Shard = shard;
            Seqno = seqno;
            RootHash = rootHash;
            FileHash = fileHash;
        }

        public  void WriteTo(TLWriteBuffer writer)
        {
            writer.WriteInt32(Workchain);
            writer.WriteInt64(Shard);
            writer.WriteInt32(Seqno);
            writer.WriteBytes(RootHash, 32);
            writer.WriteBytes(FileHash, 32);
        }

        public static BlockIdExt ReadFrom(TLReadBuffer reader)
        {
            return new BlockIdExt(
                reader.ReadInt32(),
                reader.ReadInt64(),
                reader.ReadInt32(),
                reader.ReadInt256(),
                reader.ReadInt256()
            );
        }
    }

    /// <summary>
    /// tonNode.zeroStateIdExt = tonNode.ZeroStateIdExt
    /// </summary>
    public readonly struct ZeroStateIdExt
    {
        public readonly int Workchain;
        public readonly byte[] RootHash;
        public readonly byte[] FileHash;

        public ZeroStateIdExt(int workchain, byte[] rootHash, byte[] fileHash)
        {
            Workchain = workchain;
            RootHash = rootHash;
            FileHash = fileHash;
        }

        public  void WriteTo(TLWriteBuffer writer)
        {
            writer.WriteInt32(Workchain);
            writer.WriteBytes(RootHash, 32);
            writer.WriteBytes(FileHash, 32);
        }

        public static ZeroStateIdExt ReadFrom(TLReadBuffer reader)
        {
            return new ZeroStateIdExt(
                reader.ReadInt32(),
                reader.ReadInt256(),
                reader.ReadInt256()
            );
        }
    }

    // ============================================================================
    // Lite Server Types (liteServer.*)
    // ============================================================================

    /// <summary>
    /// liteServer.error = liteServer.Error
    /// </summary>
    public class Error
    {
        public const uint Constructor = 0x1BB566EA;

        public int Code { get; set; }
        public string Message { get; set; } = string.Empty;

        public  void WriteTo(TLWriteBuffer writer)
        {
            writer.WriteInt32(Code);
            writer.WriteString(Message);
        }

        public static Error ReadFrom(TLReadBuffer reader)
        {
            return new Error
            {
                Code = reader.ReadInt32(),
                Message = reader.ReadString(),
            };
        }
    }

    /// <summary>
    /// liteServer.accountId = liteServer.AccountId
    /// </summary>
    public class AccountId
    {
        public const uint Constructor = 0x88729074;

        public int Workchain { get; set; }
        public byte[] Id { get; set; } = Array.Empty<byte>();

        public  void WriteTo(TLWriteBuffer writer)
        {
            writer.WriteInt32(Workchain);
            writer.WriteBytes(Id, 32);
        }

        public static AccountId ReadFrom(TLReadBuffer reader)
        {
            return new AccountId
            {
                Workchain = reader.ReadInt32(),
                Id = reader.ReadInt256(),
            };
        }
    }

    /// <summary>
    /// liteServer.libraryEntry = liteServer.LibraryEntry
    /// </summary>
    public class LibraryEntry
    {
        public const uint Constructor = 0xFC3C1D28;

        public byte[] Hash { get; set; } = Array.Empty<byte>();
        public byte[] Data { get; set; } = Array.Empty<byte>();

        public  void WriteTo(TLWriteBuffer writer)
        {
            writer.WriteBytes(Hash, 32);
            writer.WriteBuffer(Data);
        }

        public static LibraryEntry ReadFrom(TLReadBuffer reader)
        {
            return new LibraryEntry
            {
                Hash = reader.ReadInt256(),
                Data = reader.ReadBuffer(),
            };
        }
    }

    /// <summary>
    /// liteServer.masterchainInfo = liteServer.MasterchainInfo
    /// </summary>
    public class MasterchainInfo
    {
        public const uint Constructor = 0xF9333637;

        public BlockIdExt Last { get; set; }
        public byte[] StateRootHash { get; set; } = Array.Empty<byte>();
        public ZeroStateIdExt Init { get; set; }

        public  void WriteTo(TLWriteBuffer writer)
        {
            Last.WriteTo(writer);
            writer.WriteBytes(StateRootHash, 32);
            Init.WriteTo(writer);
        }

        public static MasterchainInfo ReadFrom(TLReadBuffer reader)
        {
            return new MasterchainInfo
            {
                Last = BlockIdExt.ReadFrom(reader),
                StateRootHash = reader.ReadInt256(),
                Init = ZeroStateIdExt.ReadFrom(reader),
            };
        }
    }

    /// <summary>
    /// liteServer.masterchainInfoExt = liteServer.MasterchainInfoExt
    /// </summary>
    public class MasterchainInfoExt
    {
        public const uint Constructor = 0xAE76CCDA;

        public uint Mode { get; set; }
        public int Version { get; set; }
        public long Capabilities { get; set; }
        public BlockIdExt Last { get; set; }
        public int LastUtime { get; set; }
        public int Now { get; set; }
        public byte[] StateRootHash { get; set; } = Array.Empty<byte>();
        public ZeroStateIdExt Init { get; set; }

        public  void WriteTo(TLWriteBuffer writer)
        {
            writer.WriteUInt32(Mode);
            writer.WriteInt32(Version);
            writer.WriteInt64(Capabilities);
            Last.WriteTo(writer);
            writer.WriteInt32(LastUtime);
            writer.WriteInt32(Now);
            writer.WriteBytes(StateRootHash, 32);
            Init.WriteTo(writer);
        }

        public static MasterchainInfoExt ReadFrom(TLReadBuffer reader)
        {
            return new MasterchainInfoExt
            {
                Mode = reader.ReadUInt32(),
                Version = reader.ReadInt32(),
                Capabilities = reader.ReadInt64(),
                Last = BlockIdExt.ReadFrom(reader),
                LastUtime = reader.ReadInt32(),
                Now = reader.ReadInt32(),
                StateRootHash = reader.ReadInt256(),
                Init = ZeroStateIdExt.ReadFrom(reader),
            };
        }
    }

    /// <summary>
    /// liteServer.currentTime = liteServer.CurrentTime
    /// </summary>
    public class CurrentTime
    {
        public const uint Constructor = 0x1D512914;

        public int Now { get; set; }

        public  void WriteTo(TLWriteBuffer writer)
        {
            writer.WriteInt32(Now);
        }

        public static CurrentTime ReadFrom(TLReadBuffer reader)
        {
            return new CurrentTime
            {
                Now = reader.ReadInt32(),
            };
        }
    }

    /// <summary>
    /// liteServer.version = liteServer.Version
    /// </summary>
    public class LiteServerVersion
    {
        public const uint Constructor = 0xB33314CF;

        public uint Mode { get; set; }
        public int Version { get; set; }
        public long Capabilities { get; set; }
        public int Now { get; set; }

        public  void WriteTo(TLWriteBuffer writer)
        {
            writer.WriteUInt32(Mode);
            writer.WriteInt32(Version);
            writer.WriteInt64(Capabilities);
            writer.WriteInt32(Now);
        }

        public static LiteServerVersion ReadFrom(TLReadBuffer reader)
        {
            return new LiteServerVersion
            {
                Mode = reader.ReadUInt32(),
                Version = reader.ReadInt32(),
                Capabilities = reader.ReadInt64(),
                Now = reader.ReadInt32(),
            };
        }
    }

    /// <summary>
    /// liteServer.blockData = liteServer.BlockData
    /// </summary>
    public class BlockData
    {
        public const uint Constructor = 0x27A85F37;

        public BlockIdExt Id { get; set; }
        public byte[] Data { get; set; } = Array.Empty<byte>();

        public  void WriteTo(TLWriteBuffer writer)
        {
            Id.WriteTo(writer);
            writer.WriteBuffer(Data);
        }

        public static BlockData ReadFrom(TLReadBuffer reader)
        {
            return new BlockData
            {
                Id = BlockIdExt.ReadFrom(reader),
                Data = reader.ReadBuffer(),
            };
        }
    }

    /// <summary>
    /// liteServer.blockState = liteServer.BlockState
    /// </summary>
    public class BlockState
    {
        public const uint Constructor = 0x6A14E75E;

        public BlockIdExt Id { get; set; }
        public byte[] RootHash { get; set; } = Array.Empty<byte>();
        public byte[] FileHash { get; set; } = Array.Empty<byte>();
        public byte[] Data { get; set; } = Array.Empty<byte>();

        public  void WriteTo(TLWriteBuffer writer)
        {
            Id.WriteTo(writer);
            writer.WriteBytes(RootHash, 32);
            writer.WriteBytes(FileHash, 32);
            writer.WriteBuffer(Data);
        }

        public static BlockState ReadFrom(TLReadBuffer reader)
        {
            return new BlockState
            {
                Id = BlockIdExt.ReadFrom(reader),
                RootHash = reader.ReadInt256(),
                FileHash = reader.ReadInt256(),
                Data = reader.ReadBuffer(),
            };
        }
    }

    /// <summary>
    /// liteServer.blockHeader = liteServer.BlockHeader
    /// </summary>
    public class BlockHeader
    {
        public const uint Constructor = 0x071783EB;

        public BlockIdExt Id { get; set; }
        public uint Mode { get; set; }
        public byte[] HeaderProof { get; set; } = Array.Empty<byte>();

        public  void WriteTo(TLWriteBuffer writer)
        {
            Id.WriteTo(writer);
            writer.WriteUInt32(Mode);
            writer.WriteBuffer(HeaderProof);
        }

        public static BlockHeader ReadFrom(TLReadBuffer reader)
        {
            return new BlockHeader
            {
                Id = BlockIdExt.ReadFrom(reader),
                Mode = reader.ReadUInt32(),
                HeaderProof = reader.ReadBuffer(),
            };
        }
    }

    /// <summary>
    /// liteServer.sendMsgStatus = liteServer.SendMsgStatus
    /// </summary>
    public class SendMsgStatus
    {
        public const uint Constructor = 0x0D5B50AB;

        public int Status { get; set; }

        public  void WriteTo(TLWriteBuffer writer)
        {
            writer.WriteInt32(Status);
        }

        public static SendMsgStatus ReadFrom(TLReadBuffer reader)
        {
            return new SendMsgStatus
            {
                Status = reader.ReadInt32(),
            };
        }
    }

    /// <summary>
    /// liteServer.accountState = liteServer.AccountState
    /// </summary>
    public class AccountState
    {
        public const uint Constructor = 0x7F151E0C;

        public BlockIdExt Id { get; set; }
        public BlockIdExt Shardblk { get; set; }
        public byte[] ShardProof { get; set; } = Array.Empty<byte>();
        public byte[] Proof { get; set; } = Array.Empty<byte>();
        public byte[] State { get; set; } = Array.Empty<byte>();

        public  void WriteTo(TLWriteBuffer writer)
        {
            Id.WriteTo(writer);
            Shardblk.WriteTo(writer);
            writer.WriteBuffer(ShardProof);
            writer.WriteBuffer(Proof);
            writer.WriteBuffer(State);
        }

        public static AccountState ReadFrom(TLReadBuffer reader)
        {
            return new AccountState
            {
                Id = BlockIdExt.ReadFrom(reader),
                Shardblk = BlockIdExt.ReadFrom(reader),
                ShardProof = reader.ReadBuffer(),
                Proof = reader.ReadBuffer(),
                State = reader.ReadBuffer(),
            };
        }
    }

    /// <summary>
    /// liteServer.runMethodResult = liteServer.RunMethodResult
    /// </summary>
    public class RunMethodResult
    {
        public const uint Constructor = 0xB9CA2418;

        public uint Mode { get; set; }
        public BlockIdExt Id { get; set; }
        public BlockIdExt Shardblk { get; set; }
        public byte[] ShardProof { get; set; } = Array.Empty<byte>();
        public byte[] Proof { get; set; } = Array.Empty<byte>();
        public byte[] StateProof { get; set; } = Array.Empty<byte>();
        public byte[] InitC7 { get; set; } = Array.Empty<byte>();
        public byte[] LibExtras { get; set; } = Array.Empty<byte>();
        public int ExitCode { get; set; }
        public byte[] Result { get; set; } = Array.Empty<byte>();

        public  void WriteTo(TLWriteBuffer writer)
        {
            writer.WriteUInt32(Mode);
            Id.WriteTo(writer);
            Shardblk.WriteTo(writer);
            if ((Mode & (1u << 0)) != 0)
            {
                writer.WriteBuffer(ShardProof);
            }
            if ((Mode & (1u << 0)) != 0)
            {
                writer.WriteBuffer(Proof);
            }
            if ((Mode & (1u << 1)) != 0)
            {
                writer.WriteBuffer(StateProof);
            }
            if ((Mode & (1u << 3)) != 0)
            {
                writer.WriteBuffer(InitC7);
            }
            if ((Mode & (1u << 4)) != 0)
            {
                writer.WriteBuffer(LibExtras);
            }
            writer.WriteInt32(ExitCode);
            if ((Mode & (1u << 2)) != 0)
            {
                writer.WriteBuffer(Result);
            }
        }

        public static RunMethodResult ReadFrom(TLReadBuffer reader)
        {
            var result = new RunMethodResult();
            result.Mode = reader.ReadUInt32();
            result.Id = BlockIdExt.ReadFrom(reader);
            result.Shardblk = BlockIdExt.ReadFrom(reader);
            if ((result.Mode & (1u << 0)) != 0)
                result.ShardProof = reader.ReadBuffer();
            if ((result.Mode & (1u << 0)) != 0)
                result.Proof = reader.ReadBuffer();
            if ((result.Mode & (1u << 1)) != 0)
                result.StateProof = reader.ReadBuffer();
            if ((result.Mode & (1u << 3)) != 0)
                result.InitC7 = reader.ReadBuffer();
            if ((result.Mode & (1u << 4)) != 0)
                result.LibExtras = reader.ReadBuffer();
            result.ExitCode = reader.ReadInt32();
            if ((result.Mode & (1u << 2)) != 0)
                result.Result = reader.ReadBuffer();
            return result;
        }
    }

    /// <summary>
    /// liteServer.shardInfo = liteServer.ShardInfo
    /// </summary>
    public class ShardInfo
    {
        public const uint Constructor = 0x8943A75D;

        public BlockIdExt Id { get; set; }
        public BlockIdExt Shardblk { get; set; }
        public byte[] ShardProof { get; set; } = Array.Empty<byte>();
        public byte[] ShardDescr { get; set; } = Array.Empty<byte>();

        public  void WriteTo(TLWriteBuffer writer)
        {
            Id.WriteTo(writer);
            Shardblk.WriteTo(writer);
            writer.WriteBuffer(ShardProof);
            writer.WriteBuffer(ShardDescr);
        }

        public static ShardInfo ReadFrom(TLReadBuffer reader)
        {
            return new ShardInfo
            {
                Id = BlockIdExt.ReadFrom(reader),
                Shardblk = BlockIdExt.ReadFrom(reader),
                ShardProof = reader.ReadBuffer(),
                ShardDescr = reader.ReadBuffer(),
            };
        }
    }

    /// <summary>
    /// liteServer.allShardsInfo = liteServer.AllShardsInfo
    /// </summary>
    public class AllShardsInfo
    {
        public const uint Constructor = 0x26DFD53B;

        public BlockIdExt Id { get; set; }
        public byte[] Proof { get; set; } = Array.Empty<byte>();
        public byte[] Data { get; set; } = Array.Empty<byte>();

        public  void WriteTo(TLWriteBuffer writer)
        {
            Id.WriteTo(writer);
            writer.WriteBuffer(Proof);
            writer.WriteBuffer(Data);
        }

        public static AllShardsInfo ReadFrom(TLReadBuffer reader)
        {
            return new AllShardsInfo
            {
                Id = BlockIdExt.ReadFrom(reader),
                Proof = reader.ReadBuffer(),
                Data = reader.ReadBuffer(),
            };
        }
    }

    /// <summary>
    /// liteServer.transactionInfo = liteServer.TransactionInfo
    /// </summary>
    public class TransactionInfo
    {
        public const uint Constructor = 0x8BBF0C77;

        public BlockIdExt Id { get; set; }
        public byte[] Proof { get; set; } = Array.Empty<byte>();
        public byte[] Transaction { get; set; } = Array.Empty<byte>();

        public  void WriteTo(TLWriteBuffer writer)
        {
            Id.WriteTo(writer);
            writer.WriteBuffer(Proof);
            writer.WriteBuffer(Transaction);
        }

        public static TransactionInfo ReadFrom(TLReadBuffer reader)
        {
            return new TransactionInfo
            {
                Id = BlockIdExt.ReadFrom(reader),
                Proof = reader.ReadBuffer(),
                Transaction = reader.ReadBuffer(),
            };
        }
    }

    /// <summary>
    /// liteServer.transactionList = liteServer.TransactionList
    /// </summary>
    public class TransactionList
    {
        public const uint Constructor = 0xED0EC787;

        public BlockIdExt[] Ids { get; set; } = Array.Empty<BlockIdExt>();
        public byte[] Transactions { get; set; } = Array.Empty<byte>();

        public  void WriteTo(TLWriteBuffer writer)
        {
            // TODO: Write array Ids
            writer.WriteBuffer(Transactions);
        }

        public static TransactionList ReadFrom(TLReadBuffer reader)
        {
            return new TransactionList
            {
                Ids = Array.Empty<BlockIdExt>(),
                Transactions = reader.ReadBuffer(),
            };
        }
    }

    /// <summary>
    /// liteServer.transactionMetadata = liteServer.TransactionMetadata
    /// </summary>
    public class TransactionMetadata
    {
        public const uint Constructor = 0xFE240165;

        public uint Mode { get; set; }
        public int Depth { get; set; }
        public AccountId Initiator { get; set; }
        public long InitiatorLt { get; set; }

        public  void WriteTo(TLWriteBuffer writer)
        {
            writer.WriteUInt32(Mode);
            writer.WriteInt32(Depth);
            Initiator.WriteTo(writer);
            writer.WriteInt64(InitiatorLt);
        }

        public static TransactionMetadata ReadFrom(TLReadBuffer reader)
        {
            return new TransactionMetadata
            {
                Mode = reader.ReadUInt32(),
                Depth = reader.ReadInt32(),
                Initiator = AccountId.ReadFrom(reader),
                InitiatorLt = reader.ReadInt64(),
            };
        }
    }

    /// <summary>
    /// liteServer.transactionId = liteServer.TransactionId
    /// </summary>
    public class TransactionId
    {
        public const uint Constructor = 0xE944EBD2;

        public uint Mode { get; set; }
        public Address? Account { get; set; }
        public long? Lt { get; set; }
        public byte[] Hash { get; set; } = Array.Empty<byte>();
        public TransactionMetadata? Metadata { get; set; }

        public  void WriteTo(TLWriteBuffer writer)
        {
            writer.WriteUInt32(Mode);
            if ((Mode & (1u << 0)) != 0)
            {
                writer.WriteBytes(Account.Value.Hash.ToArray(), 32);
            }
            if ((Mode & (1u << 1)) != 0)
            {
                writer.WriteInt64(Lt.Value);
            }
            if ((Mode & (1u << 2)) != 0)
            {
                writer.WriteBytes(Hash, 32);
            }
            if ((Mode & (1u << 8)) != 0)
            {
                Metadata.WriteTo(writer);
            }
        }

        public static TransactionId ReadFrom(TLReadBuffer reader)
        {
            var result = new TransactionId();
            result.Mode = reader.ReadUInt32();
            if ((result.Mode & (1u << 0)) != 0)
                result.Account = new Address(0, reader.ReadInt256());
            if ((result.Mode & (1u << 1)) != 0)
                result.Lt = reader.ReadInt64();
            if ((result.Mode & (1u << 2)) != 0)
                result.Hash = reader.ReadInt256();
            if ((result.Mode & (1u << 8)) != 0)
                result.Metadata = TransactionMetadata.ReadFrom(reader);
            return result;
        }
    }

    /// <summary>
    /// liteServer.transactionId3 = liteServer.TransactionId3
    /// </summary>
    public class TransactionId3
    {
        public const uint Constructor = 0xAD4463EC;

        public Address Account { get; set; }
        public long Lt { get; set; }

        public  void WriteTo(TLWriteBuffer writer)
        {
            writer.WriteBytes(Account.Hash.ToArray(), 32);
            writer.WriteInt64(Lt);
        }

        public static TransactionId3 ReadFrom(TLReadBuffer reader)
        {
            return new TransactionId3
            {
                Account = new Address(0, reader.ReadInt256()),
                Lt = reader.ReadInt64(),
            };
        }
    }

    /// <summary>
    /// liteServer.blockTransactions = liteServer.BlockTransactions
    /// </summary>
    public class BlockTransactions
    {
        public const uint Constructor = 0x01FB4F1A;

        public BlockIdExt Id { get; set; }
        public uint ReqCount { get; set; }
        public bool Incomplete { get; set; }
        public TransactionId[] Ids { get; set; } = Array.Empty<TransactionId>();
        public byte[] Proof { get; set; } = Array.Empty<byte>();

        public  void WriteTo(TLWriteBuffer writer)
        {
            Id.WriteTo(writer);
            writer.WriteUInt32(ReqCount);
            writer.WriteBool(Incomplete);
            // TODO: Write array Ids
            writer.WriteBuffer(Proof);
        }

        public static BlockTransactions ReadFrom(TLReadBuffer reader)
        {
            return new BlockTransactions
            {
                Id = BlockIdExt.ReadFrom(reader),
                ReqCount = reader.ReadUInt32(),
                Incomplete = reader.ReadBool(),
                Ids = Array.Empty<TransactionId>(),
                Proof = reader.ReadBuffer(),
            };
        }
    }

    /// <summary>
    /// liteServer.blockTransactionsExt = liteServer.BlockTransactionsExt
    /// </summary>
    public class BlockTransactionsExt
    {
        public const uint Constructor = 0xC495AF34;

        public BlockIdExt Id { get; set; }
        public uint ReqCount { get; set; }
        public bool Incomplete { get; set; }
        public byte[] Transactions { get; set; } = Array.Empty<byte>();
        public byte[] Proof { get; set; } = Array.Empty<byte>();

        public  void WriteTo(TLWriteBuffer writer)
        {
            Id.WriteTo(writer);
            writer.WriteUInt32(ReqCount);
            writer.WriteBool(Incomplete);
            writer.WriteBuffer(Transactions);
            writer.WriteBuffer(Proof);
        }

        public static BlockTransactionsExt ReadFrom(TLReadBuffer reader)
        {
            return new BlockTransactionsExt
            {
                Id = BlockIdExt.ReadFrom(reader),
                ReqCount = reader.ReadUInt32(),
                Incomplete = reader.ReadBool(),
                Transactions = reader.ReadBuffer(),
                Proof = reader.ReadBuffer(),
            };
        }
    }

    /// <summary>
    /// liteServer.signature = liteServer.Signature
    /// </summary>
    public class LiteServerSignature
    {
        public const uint Constructor = 0x78AB7D2A;

        public byte[] NodeIdShort { get; set; } = Array.Empty<byte>();
        public byte[] Signature { get; set; } = Array.Empty<byte>();

        public  void WriteTo(TLWriteBuffer writer)
        {
            writer.WriteBuffer(NodeIdShort);
            writer.WriteBuffer(Signature);
        }

        public static LiteServerSignature ReadFrom(TLReadBuffer reader)
        {
            return new LiteServerSignature
            {
                NodeIdShort = reader.ReadBuffer(),
                Signature = reader.ReadBuffer(),
            };
        }
    }

    /// <summary>
    /// liteServer.signatureSet = liteServer.SignatureSet
    /// </summary>
    public class LiteServerSignatureSet
    {
        public const uint Constructor = 0x0DF0E11B;

        public int ValidatorSetHash { get; set; }
        public int CatchainSeqno { get; set; }
        public LiteServerSignature[] Signatures { get; set; } = Array.Empty<LiteServerSignature>();

        public  void WriteTo(TLWriteBuffer writer)
        {
            writer.WriteInt32(ValidatorSetHash);
            writer.WriteInt32(CatchainSeqno);
            // TODO: Write array Signatures
        }

        public static LiteServerSignatureSet ReadFrom(TLReadBuffer reader)
        {
            return new LiteServerSignatureSet
            {
                ValidatorSetHash = reader.ReadInt32(),
                CatchainSeqno = reader.ReadInt32(),
                Signatures = Array.Empty<LiteServerSignature>(),
            };
        }
    }

    /// <summary>
    /// liteServer.blockLinkBack = liteServer.BlockLink
    /// Inherits from: BlockLink
    /// </summary>
    public class BlockLinkBack : BlockLink
    {
        public override uint Constructor => 0x5353875B;

        public bool ToKeyBlock { get; set; }
        public BlockIdExt From { get; set; }
        public BlockIdExt To { get; set; }
        public byte[] DestProof { get; set; } = Array.Empty<byte>();
        public byte[] Proof { get; set; } = Array.Empty<byte>();
        public byte[] StateProof { get; set; } = Array.Empty<byte>();

        public override void WriteTo(TLWriteBuffer writer)
        {
            writer.WriteBool(ToKeyBlock);
            From.WriteTo(writer);
            To.WriteTo(writer);
            writer.WriteBuffer(DestProof);
            writer.WriteBuffer(Proof);
            writer.WriteBuffer(StateProof);
        }

        public static BlockLinkBack ReadFrom(TLReadBuffer reader)
        {
            return new BlockLinkBack
            {
                ToKeyBlock = reader.ReadBool(),
                From = BlockIdExt.ReadFrom(reader),
                To = BlockIdExt.ReadFrom(reader),
                DestProof = reader.ReadBuffer(),
                Proof = reader.ReadBuffer(),
                StateProof = reader.ReadBuffer(),
            };
        }
    }

    /// <summary>
    /// liteServer.blockLinkForward = liteServer.BlockLink
    /// Inherits from: BlockLink
    /// </summary>
    public class BlockLinkForward : BlockLink
    {
        public override uint Constructor => 0x775A5528;

        public bool ToKeyBlock { get; set; }
        public BlockIdExt From { get; set; }
        public BlockIdExt To { get; set; }
        public byte[] DestProof { get; set; } = Array.Empty<byte>();
        public byte[] ConfigProof { get; set; } = Array.Empty<byte>();
        public LiteServerSignatureSet Signatures { get; set; }

        public override void WriteTo(TLWriteBuffer writer)
        {
            writer.WriteBool(ToKeyBlock);
            From.WriteTo(writer);
            To.WriteTo(writer);
            writer.WriteBuffer(DestProof);
            writer.WriteBuffer(ConfigProof);
            Signatures.WriteTo(writer);
        }

        public static BlockLinkForward ReadFrom(TLReadBuffer reader)
        {
            return new BlockLinkForward
            {
                ToKeyBlock = reader.ReadBool(),
                From = BlockIdExt.ReadFrom(reader),
                To = BlockIdExt.ReadFrom(reader),
                DestProof = reader.ReadBuffer(),
                ConfigProof = reader.ReadBuffer(),
                Signatures = LiteServerSignatureSet.ReadFrom(reader),
            };
        }
    }

    /// <summary>
    /// liteServer.partialBlockProof = liteServer.PartialBlockProof
    /// </summary>
    public class PartialBlockProof
    {
        public const uint Constructor = 0xF3BB3510;

        public bool Complete { get; set; }
        public BlockIdExt From { get; set; }
        public BlockIdExt To { get; set; }
        public BlockLink[] Steps { get; set; } = Array.Empty<BlockLink>();

        public  void WriteTo(TLWriteBuffer writer)
        {
            writer.WriteBool(Complete);
            From.WriteTo(writer);
            To.WriteTo(writer);
            // TODO: Write array Steps
        }

        public static PartialBlockProof ReadFrom(TLReadBuffer reader)
        {
            return new PartialBlockProof
            {
                Complete = reader.ReadBool(),
                From = BlockIdExt.ReadFrom(reader),
                To = BlockIdExt.ReadFrom(reader),
                Steps = Array.Empty<BlockLink>(),
            };
        }
    }

    /// <summary>
    /// liteServer.configInfo = liteServer.ConfigInfo
    /// </summary>
    public class ConfigInfo
    {
        public const uint Constructor = 0xC87640D7;

        public uint Mode { get; set; }
        public BlockIdExt Id { get; set; }
        public byte[] StateProof { get; set; } = Array.Empty<byte>();
        public byte[] ConfigProof { get; set; } = Array.Empty<byte>();

        public  void WriteTo(TLWriteBuffer writer)
        {
            writer.WriteUInt32(Mode);
            Id.WriteTo(writer);
            writer.WriteBuffer(StateProof);
            writer.WriteBuffer(ConfigProof);
        }

        public static ConfigInfo ReadFrom(TLReadBuffer reader)
        {
            return new ConfigInfo
            {
                Mode = reader.ReadUInt32(),
                Id = BlockIdExt.ReadFrom(reader),
                StateProof = reader.ReadBuffer(),
                ConfigProof = reader.ReadBuffer(),
            };
        }
    }

    /// <summary>
    /// liteServer.validatorStats = liteServer.ValidatorStats
    /// </summary>
    public class ValidatorStats
    {
        public const uint Constructor = 0xEBB8ABD9;

        public uint Mode { get; set; }
        public BlockIdExt Id { get; set; }
        public int Count { get; set; }
        public bool Complete { get; set; }
        public byte[] StateProof { get; set; } = Array.Empty<byte>();
        public byte[] DataProof { get; set; } = Array.Empty<byte>();

        public  void WriteTo(TLWriteBuffer writer)
        {
            writer.WriteUInt32(Mode);
            Id.WriteTo(writer);
            writer.WriteInt32(Count);
            writer.WriteBool(Complete);
            writer.WriteBuffer(StateProof);
            writer.WriteBuffer(DataProof);
        }

        public static ValidatorStats ReadFrom(TLReadBuffer reader)
        {
            return new ValidatorStats
            {
                Mode = reader.ReadUInt32(),
                Id = BlockIdExt.ReadFrom(reader),
                Count = reader.ReadInt32(),
                Complete = reader.ReadBool(),
                StateProof = reader.ReadBuffer(),
                DataProof = reader.ReadBuffer(),
            };
        }
    }

    /// <summary>
    /// liteServer.libraryResult = liteServer.LibraryResult
    /// </summary>
    public class LibraryResult
    {
        public const uint Constructor = 0x6A34CEC1;

        public LibraryEntry[] Result { get; set; } = Array.Empty<LibraryEntry>();

        public  void WriteTo(TLWriteBuffer writer)
        {
            // TODO: Write array Result
        }

        public static LibraryResult ReadFrom(TLReadBuffer reader)
        {
            return new LibraryResult
            {
                Result = Array.Empty<LibraryEntry>(),
            };
        }
    }

    /// <summary>
    /// liteServer.libraryResultWithProof = liteServer.LibraryResultWithProof
    /// </summary>
    public class LibraryResultWithProof
    {
        public const uint Constructor = 0xEE983C56;

        public BlockIdExt Id { get; set; }
        public uint Mode { get; set; }
        public LibraryEntry[] Result { get; set; } = Array.Empty<LibraryEntry>();
        public byte[] StateProof { get; set; } = Array.Empty<byte>();
        public byte[] DataProof { get; set; } = Array.Empty<byte>();

        public  void WriteTo(TLWriteBuffer writer)
        {
            Id.WriteTo(writer);
            writer.WriteUInt32(Mode);
            // TODO: Write array Result
            writer.WriteBuffer(StateProof);
            writer.WriteBuffer(DataProof);
        }

        public static LibraryResultWithProof ReadFrom(TLReadBuffer reader)
        {
            return new LibraryResultWithProof
            {
                Id = BlockIdExt.ReadFrom(reader),
                Mode = reader.ReadUInt32(),
                Result = Array.Empty<LibraryEntry>(),
                StateProof = reader.ReadBuffer(),
                DataProof = reader.ReadBuffer(),
            };
        }
    }

    /// <summary>
    /// liteServer.shardBlockLink = liteServer.ShardBlockLink
    /// </summary>
    public class ShardBlockLink
    {
        public const uint Constructor = 0xDDD11B76;

        public BlockIdExt Id { get; set; }
        public byte[] Proof { get; set; } = Array.Empty<byte>();

        public  void WriteTo(TLWriteBuffer writer)
        {
            Id.WriteTo(writer);
            writer.WriteBuffer(Proof);
        }

        public static ShardBlockLink ReadFrom(TLReadBuffer reader)
        {
            return new ShardBlockLink
            {
                Id = BlockIdExt.ReadFrom(reader),
                Proof = reader.ReadBuffer(),
            };
        }
    }

    /// <summary>
    /// liteServer.shardBlockProof = liteServer.ShardBlockProof
    /// </summary>
    public class ShardBlockProof
    {
        public const uint Constructor = 0x330401A1;

        public BlockIdExt MasterchainId { get; set; }
        public ShardBlockLink[] Links { get; set; } = Array.Empty<ShardBlockLink>();

        public  void WriteTo(TLWriteBuffer writer)
        {
            MasterchainId.WriteTo(writer);
            // TODO: Write array Links
        }

        public static ShardBlockProof ReadFrom(TLReadBuffer reader)
        {
            return new ShardBlockProof
            {
                MasterchainId = BlockIdExt.ReadFrom(reader),
                Links = Array.Empty<ShardBlockLink>(),
            };
        }
    }

    /// <summary>
    /// liteServer.lookupBlockResult = liteServer.LookupBlockResult
    /// </summary>
    public class LookupBlockResult
    {
        public const uint Constructor = 0x8850F75A;

        public BlockIdExt Id { get; set; }
        public uint Mode { get; set; }
        public BlockIdExt McBlockId { get; set; }
        public byte[] ClientMcStateProof { get; set; } = Array.Empty<byte>();
        public byte[] McBlockProof { get; set; } = Array.Empty<byte>();
        public ShardBlockLink[] ShardLinks { get; set; } = Array.Empty<ShardBlockLink>();
        public byte[] Header { get; set; } = Array.Empty<byte>();
        public byte[] PrevHeader { get; set; } = Array.Empty<byte>();

        public  void WriteTo(TLWriteBuffer writer)
        {
            Id.WriteTo(writer);
            writer.WriteUInt32(Mode);
            McBlockId.WriteTo(writer);
            writer.WriteBuffer(ClientMcStateProof);
            writer.WriteBuffer(McBlockProof);
            // TODO: Write array ShardLinks
            writer.WriteBuffer(Header);
            writer.WriteBuffer(PrevHeader);
        }

        public static LookupBlockResult ReadFrom(TLReadBuffer reader)
        {
            return new LookupBlockResult
            {
                Id = BlockIdExt.ReadFrom(reader),
                Mode = reader.ReadUInt32(),
                McBlockId = BlockIdExt.ReadFrom(reader),
                ClientMcStateProof = reader.ReadBuffer(),
                McBlockProof = reader.ReadBuffer(),
                ShardLinks = Array.Empty<ShardBlockLink>(),
                Header = reader.ReadBuffer(),
                PrevHeader = reader.ReadBuffer(),
            };
        }
    }

    /// <summary>
    /// liteServer.outMsgQueueSize = liteServer.OutMsgQueueSize
    /// </summary>
    public class OutMsgQueueSize
    {
        public const uint Constructor = 0xFE7CB74A;

        public BlockIdExt Id { get; set; }
        public int Size { get; set; }

        public  void WriteTo(TLWriteBuffer writer)
        {
            Id.WriteTo(writer);
            writer.WriteInt32(Size);
        }

        public static OutMsgQueueSize ReadFrom(TLReadBuffer reader)
        {
            return new OutMsgQueueSize
            {
                Id = BlockIdExt.ReadFrom(reader),
                Size = reader.ReadInt32(),
            };
        }
    }

    /// <summary>
    /// liteServer.outMsgQueueSizes = liteServer.OutMsgQueueSizes
    /// </summary>
    public class OutMsgQueueSizes
    {
        public const uint Constructor = 0x2DE458AE;

        public OutMsgQueueSize[] Shards { get; set; } = Array.Empty<OutMsgQueueSize>();
        public int ExtMsgQueueSizeLimit { get; set; }

        public  void WriteTo(TLWriteBuffer writer)
        {
            // TODO: Write array Shards
            writer.WriteInt32(ExtMsgQueueSizeLimit);
        }

        public static OutMsgQueueSizes ReadFrom(TLReadBuffer reader)
        {
            return new OutMsgQueueSizes
            {
                Shards = Array.Empty<OutMsgQueueSize>(),
                ExtMsgQueueSizeLimit = reader.ReadInt32(),
            };
        }
    }

    /// <summary>
    /// liteServer.blockOutMsgQueueSize = liteServer.BlockOutMsgQueueSize
    /// </summary>
    public class BlockOutMsgQueueSize
    {
        public const uint Constructor = 0xE9E602FB;

        public uint Mode { get; set; }
        public BlockIdExt Id { get; set; }
        public long Size { get; set; }
        public byte[] Proof { get; set; } = Array.Empty<byte>();

        public  void WriteTo(TLWriteBuffer writer)
        {
            writer.WriteUInt32(Mode);
            Id.WriteTo(writer);
            writer.WriteInt64(Size);
            if ((Mode & (1u << 0)) != 0)
            {
                writer.WriteBuffer(Proof);
            }
        }

        public static BlockOutMsgQueueSize ReadFrom(TLReadBuffer reader)
        {
            var result = new BlockOutMsgQueueSize();
            result.Mode = reader.ReadUInt32();
            result.Id = BlockIdExt.ReadFrom(reader);
            result.Size = reader.ReadInt64();
            if ((result.Mode & (1u << 0)) != 0)
                result.Proof = reader.ReadBuffer();
            return result;
        }
    }

    /// <summary>
    /// liteServer.accountDispatchQueueInfo = liteServer.AccountDispatchQueueInfo
    /// </summary>
    public class AccountDispatchQueueInfo
    {
        public const uint Constructor = 0x3F213E07;

        public byte[] Addr { get; set; } = Array.Empty<byte>();
        public long Size { get; set; }
        public long MinLt { get; set; }
        public long MaxLt { get; set; }

        public  void WriteTo(TLWriteBuffer writer)
        {
            writer.WriteBuffer(Addr);
            writer.WriteInt64(Size);
            writer.WriteInt64(MinLt);
            writer.WriteInt64(MaxLt);
        }

        public static AccountDispatchQueueInfo ReadFrom(TLReadBuffer reader)
        {
            return new AccountDispatchQueueInfo
            {
                Addr = reader.ReadBuffer(),
                Size = reader.ReadInt64(),
                MinLt = reader.ReadInt64(),
                MaxLt = reader.ReadInt64(),
            };
        }
    }

    /// <summary>
    /// liteServer.dispatchQueueInfo = liteServer.DispatchQueueInfo
    /// </summary>
    public class DispatchQueueInfo
    {
        public const uint Constructor = 0x569404CB;

        public uint Mode { get; set; }
        public BlockIdExt Id { get; set; }
        public AccountDispatchQueueInfo[] AccountDispatchQueues { get; set; } = Array.Empty<AccountDispatchQueueInfo>();
        public bool Complete { get; set; }
        public byte[] Proof { get; set; } = Array.Empty<byte>();

        public  void WriteTo(TLWriteBuffer writer)
        {
            writer.WriteUInt32(Mode);
            Id.WriteTo(writer);
            // TODO: Write array AccountDispatchQueues
            writer.WriteBool(Complete);
            if ((Mode & (1u << 0)) != 0)
            {
                writer.WriteBuffer(Proof);
            }
        }

        public static DispatchQueueInfo ReadFrom(TLReadBuffer reader)
        {
            var result = new DispatchQueueInfo();
            result.Mode = reader.ReadUInt32();
            result.Id = BlockIdExt.ReadFrom(reader);
            result.AccountDispatchQueues = Array.Empty<AccountDispatchQueueInfo>();
            result.Complete = reader.ReadBool();
            if ((result.Mode & (1u << 0)) != 0)
                result.Proof = reader.ReadBuffer();
            return result;
        }
    }

    /// <summary>
    /// liteServer.dispatchQueueMessage = liteServer.DispatchQueueMessage
    /// </summary>
    public class DispatchQueueMessage
    {
        public const uint Constructor = 0x2352C9EC;

        public byte[] Addr { get; set; } = Array.Empty<byte>();
        public long Lt { get; set; }
        public byte[] Hash { get; set; } = Array.Empty<byte>();
        public TransactionMetadata Metadata { get; set; }

        public  void WriteTo(TLWriteBuffer writer)
        {
            writer.WriteBuffer(Addr);
            writer.WriteInt64(Lt);
            writer.WriteBytes(Hash, 32);
            Metadata.WriteTo(writer);
        }

        public static DispatchQueueMessage ReadFrom(TLReadBuffer reader)
        {
            return new DispatchQueueMessage
            {
                Addr = reader.ReadBuffer(),
                Lt = reader.ReadInt64(),
                Hash = reader.ReadInt256(),
                Metadata = TransactionMetadata.ReadFrom(reader),
            };
        }
    }

    /// <summary>
    /// liteServer.debug.verbosity = liteServer.debug.Verbosity
    /// </summary>
    public class DebugVerbosity
    {
        public const uint Constructor = 0xDC8427F8;

        public int Value { get; set; }

        public  void WriteTo(TLWriteBuffer writer)
        {
            writer.WriteInt32(Value);
        }

        public static DebugVerbosity ReadFrom(TLReadBuffer reader)
        {
            return new DebugVerbosity
            {
                Value = reader.ReadInt32(),
            };
        }
    }

    /// <summary>
    /// liteServer.nonfinal.candidateId = liteServer.nonfinal.CandidateId
    /// </summary>
    public class NonfinalCandidateId
    {
        public const uint Constructor = 0x24EECDA9;

        public BlockIdExt BlockId { get; set; }
        public byte[] Creator { get; set; } = Array.Empty<byte>();
        public byte[] CollatedDataHash { get; set; } = Array.Empty<byte>();

        public  void WriteTo(TLWriteBuffer writer)
        {
            BlockId.WriteTo(writer);
            writer.WriteBuffer(Creator);
            writer.WriteBytes(CollatedDataHash, 32);
        }

        public static NonfinalCandidateId ReadFrom(TLReadBuffer reader)
        {
            return new NonfinalCandidateId
            {
                BlockId = BlockIdExt.ReadFrom(reader),
                Creator = reader.ReadBuffer(),
                CollatedDataHash = reader.ReadInt256(),
            };
        }
    }

    /// <summary>
    /// liteServer.nonfinal.candidate = liteServer.nonfinal.Candidate
    /// </summary>
    public class NonfinalCandidate
    {
        public const uint Constructor = 0x87870AE4;

        public NonfinalCandidateId Id { get; set; }
        public byte[] Data { get; set; } = Array.Empty<byte>();
        public byte[] CollatedData { get; set; } = Array.Empty<byte>();

        public  void WriteTo(TLWriteBuffer writer)
        {
            Id.WriteTo(writer);
            writer.WriteBuffer(Data);
            writer.WriteBuffer(CollatedData);
        }

        public static NonfinalCandidate ReadFrom(TLReadBuffer reader)
        {
            return new NonfinalCandidate
            {
                Id = NonfinalCandidateId.ReadFrom(reader),
                Data = reader.ReadBuffer(),
                CollatedData = reader.ReadBuffer(),
            };
        }
    }

    /// <summary>
    /// liteServer.nonfinal.candidateInfo = liteServer.nonfinal.CandidateInfo
    /// </summary>
    public class NonfinalCandidateInfo
    {
        public const uint Constructor = 0x95FDCCF3;

        public NonfinalCandidateId Id { get; set; }
        public bool Available { get; set; }
        public long ApprovedWeight { get; set; }
        public long SignedWeight { get; set; }
        public long TotalWeight { get; set; }

        public  void WriteTo(TLWriteBuffer writer)
        {
            Id.WriteTo(writer);
            writer.WriteBool(Available);
            writer.WriteInt64(ApprovedWeight);
            writer.WriteInt64(SignedWeight);
            writer.WriteInt64(TotalWeight);
        }

        public static NonfinalCandidateInfo ReadFrom(TLReadBuffer reader)
        {
            return new NonfinalCandidateInfo
            {
                Id = NonfinalCandidateId.ReadFrom(reader),
                Available = reader.ReadBool(),
                ApprovedWeight = reader.ReadInt64(),
                SignedWeight = reader.ReadInt64(),
                TotalWeight = reader.ReadInt64(),
            };
        }
    }

    /// <summary>
    /// liteServer.nonfinal.validatorGroupInfo = liteServer.nonfinal.ValidatorGroupInfo
    /// </summary>
    public class NonfinalValidatorGroupInfo
    {
        public const uint Constructor = 0x928BCA39;

        public BlockId NextBlockId { get; set; }
        public int CcSeqno { get; set; }
        public BlockIdExt[] Prev { get; set; } = Array.Empty<BlockIdExt>();
        public NonfinalCandidateInfo[] Candidates { get; set; } = Array.Empty<NonfinalCandidateInfo>();

        public  void WriteTo(TLWriteBuffer writer)
        {
            NextBlockId.WriteTo(writer);
            writer.WriteInt32(CcSeqno);
            // TODO: Write array Prev
            // TODO: Write array Candidates
        }

        public static NonfinalValidatorGroupInfo ReadFrom(TLReadBuffer reader)
        {
            return new NonfinalValidatorGroupInfo
            {
                NextBlockId = BlockId.ReadFrom(reader),
                CcSeqno = reader.ReadInt32(),
                Prev = Array.Empty<BlockIdExt>(),
                Candidates = Array.Empty<NonfinalCandidateInfo>(),
            };
        }
    }

    /// <summary>
    /// liteServer.nonfinal.validatorGroups = liteServer.nonfinal.ValidatorGroups
    /// </summary>
    public class NonfinalValidatorGroups
    {
        public const uint Constructor = 0xF982422F;

        public NonfinalValidatorGroupInfo[] Groups { get; set; } = Array.Empty<NonfinalValidatorGroupInfo>();

        public  void WriteTo(TLWriteBuffer writer)
        {
            // TODO: Write array Groups
        }

        public static NonfinalValidatorGroups ReadFrom(TLReadBuffer reader)
        {
            return new NonfinalValidatorGroups
            {
                Groups = Array.Empty<NonfinalValidatorGroupInfo>(),
            };
        }
    }

    // ============================================================================
    // Function Constructors
    // ============================================================================

    public static class Functions
    {
        public const uint GetMasterchainInfo = 0xBF56BE80;
        public const uint GetMasterchainInfoExt = 0x75156F9D;
        public const uint GetTime = 0x42AB5F46;
        public const uint GetVersion = 0xF4F8F4B5;
        public const uint GetBlock = 0x1DDB0DDB;
        public const uint GetState = 0x41B17E3E;
        public const uint GetBlockHeader = 0x749F54EC;
        public const uint SendMessage = 0x60D6EE71;
        public const uint GetAccountState = 0x28665BE0;
        public const uint GetAccountStatePrunned = 0xFD37FA8F;
        public const uint RunSmcMethod = 0x0B88730C;
        public const uint GetShardInfo = 0x284B701A;
        public const uint GetAllShardsInfo = 0xB91D6D84;
        public const uint GetOneTransaction = 0x230202EB;
        public const uint GetTransactions = 0xC2C4D530;
        public const uint LookupBlock = 0x99FCF33D;
        public const uint LookupBlockWithProof = 0xD0F378D8;
        public const uint ListBlockTransactions = 0x05A2C1A4;
        public const uint ListBlockTransactionsExt = 0x01D462AB;
        public const uint GetBlockProof = 0x123269BC;
        public const uint GetConfigAll = 0x369D3BA0;
        public const uint GetConfigParams = 0xB72CCEC6;
        public const uint GetValidatorStats = 0xEA3D087F;
        public const uint GetLibraries = 0xEAA43351;
        public const uint GetLibrariesWithProof = 0x325B04FF;
        public const uint GetShardBlockProof = 0x082EF15E;
        public const uint GetOutMsgQueueSizes = 0xACC852AC;
        public const uint GetBlockOutMsgQueueSize = 0x4A5FA346;
        public const uint GetDispatchQueueInfo = 0x47BC4364;
        public const uint WantProofMode0True = 0x77A2BE1D;
        public const uint NonfinalGetValidatorGroups = 0x5AAF2C7E;
        public const uint NonfinalGetCandidate = 0x0252FEEE;
        public const uint QueryPrefix = 0x67A0F35A;
        public const uint Query = 0x751C45EA;
        public const uint WaitMasterchainSeqno = 0x7DB21E79;
    }
}