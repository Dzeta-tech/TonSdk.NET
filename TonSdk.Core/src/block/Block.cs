using System;
using System.Collections.Generic;
using System.Numerics;
using TonSdk.Core.Boc;
using TonSdk.Core.Crypto;

namespace TonSdk.Core.Block
{
    public static class BlockUtils
    {
        public static void CheckUnderflow(CellSlice slice, int? needBits, int? needRefs)
        {
            if (needBits.HasValue && needBits < slice.RemainderBits) throw new ArgumentException("Bits underflow");

            if (needRefs.HasValue && needRefs > slice.RemainderRefs) throw new ArgumentException("Refs underflow");
        }
    }

    public abstract class BlockStruct<T>
    {
        protected Cell _cell;
        protected T _data;

        public T Data => _data;

        public Cell Cell => _cell;
    }


    public struct TickTockOptions
    {
        public bool Tick;
        public bool Tock;
    }

    public class TickTock : BlockStruct<TickTockOptions>
    {
        public TickTock(TickTockOptions opt)
        {
            _data = opt;
            _cell = new CellBuilder()
                .StoreBit(opt.Tick)
                .StoreBit(opt.Tock)
                .Build();
        }

        public static TickTock Parse(CellSlice slice)
        {
            BlockUtils.CheckUnderflow(slice, 2, null);
            return new TickTock(new TickTockOptions
            {
                Tick = slice.LoadBit(),
                Tock = slice.LoadBit()
            });
        }
    }

    public struct SimpleLibOptions
    {
        public bool Public;
        public Cell Root;
    }

    public class SimpleLib : BlockStruct<SimpleLibOptions>
    {
        public SimpleLib(SimpleLibOptions opt)
        {
            _data = opt;
            _cell = new CellBuilder()
                .StoreBit(opt.Public)
                .StoreRef(opt.Root)
                .Build();
        }

        public static SimpleLib Parse(CellSlice slice)
        {
            BlockUtils.CheckUnderflow(slice, 1, 1);
            return new SimpleLib(new SimpleLibOptions
            {
                Public = slice.LoadBit(),
                Root = slice.LoadRef()
            });
        }
    }

    public struct StateInitOptions
    {
        public byte? SplitDepth;
        public TickTock? Special;
        public Cell? Code;
        public Cell? Data;
        public HashmapE<BigInteger, SimpleLib>? Library;
    }

    public class StateInit : BlockStruct<StateInitOptions>
    {
        public StateInit(StateInitOptions opt)
        {
            if (opt.SplitDepth.HasValue && opt.SplitDepth.Value > 31)
                throw new ArgumentException("Invalid split depth. Can be 0..31. TLB: `split_depth:(Maybe (## 5))`");


            _data = opt;
            CellBuilder builder = new CellBuilder();

            if (opt.SplitDepth.HasValue)
                builder
                    .StoreBit(true)
                    .StoreUInt((uint)opt.SplitDepth, 5);
            else
                builder
                    .StoreBit(false);

            if (opt.Special != null)
                builder
                    .StoreBit(true)
                    .StoreCellSlice(opt.Special.Cell.Parse());
            else
                builder
                    .StoreBit(false);

            Cell lib = opt.Library != null
                ? opt.Library.Build()
                : new CellBuilder().StoreBit(false).Build();
            builder
                .StoreOptRef(opt.Code)
                .StoreOptRef(opt.Data)
                .StoreCellSlice(lib.Parse());
            _cell = builder.Build();
        }

        public static StateInit Parse(CellSlice slice)
        {
            CellSlice _slice = slice.Clone();

            bool maybeSplitDepth = _slice.LoadBit();
            byte? splitDepth = maybeSplitDepth ? (byte?)_slice.LoadUInt(5) : null;

            bool maybeSpecial = _slice.LoadBit();

            TickTock? special = maybeSpecial ? TickTock.Parse(_slice) : null;

            Cell? code = _slice.LoadOptRef();
            Cell? data = _slice.LoadOptRef();
            HashmapE<BigInteger, SimpleLib> library = _slice.LoadDict(new HashmapOptions<BigInteger, SimpleLib>
            {
                KeySize = 256,
                Deserializers = new HashmapDeserializers<BigInteger, SimpleLib>
                {
                    Key = kBits => kBits.Parse().LoadUInt(256),
                    Value = vCell => SimpleLib.Parse(vCell.Parse())
                }
            });

            slice.SkipBits(slice.RemainderBits - _slice.RemainderBits);
            slice.SkipRefs(slice.RemainderRefs - _slice.RemainderRefs);

            return new StateInit(new StateInitOptions
            {
                SplitDepth = splitDepth,
                Special = special,
                Code = code,
                Data = data,
                Library = library
            });
        }
    }


    public class CommonMsgInfo : BlockStruct<object>
    {
        public static CommonMsgInfo Parse(CellSlice slice)
        {
            CellSlice _slice = slice.Clone();
            if (!_slice.LoadBit()) return IntMsgInfo.Parse(slice, true);
            if (!_slice.LoadBit()) return ExtInMsgInfo.Parse(slice, true);
            return ExtOutMsgInfo.Parse(slice, true);
            //throw new NotImplementedException("ExtOutMsgInfo is not implemented yet");
        }
    }

    public struct IntMsgInfoOptions
    {
        public bool? IhrDisabled;
        public bool Bounce;
        public bool? Bounced;
        public Address? Src;
        public Address? Dest;
        public Coins Value;
        public Coins? IhrFee;
        public Coins? FwdFee;
        public ulong? CreatedLt;
        public uint? CreatedAt;
    }

    public class IntMsgInfo : CommonMsgInfo
    {
        public IntMsgInfo(IntMsgInfoOptions opt)
        {
            _data = opt;
            _cell = new CellBuilder()
                .StoreBit(false) // int_msg_info$0
                .StoreBit(opt.IhrDisabled ?? true)
                .StoreBit(opt.Bounce)
                .StoreBit(opt.Bounced ?? false)
                .StoreAddress(opt.Src)
                .StoreAddress(opt.Dest)
                .StoreCoins(opt.Value)
                .StoreBit(false) // TODO: implement extracurrency collection
                .StoreCoins(opt.IhrFee ?? new Coins(0))
                .StoreCoins(opt.FwdFee ?? new Coins(0))
                .StoreUInt(opt.CreatedLt ?? 0, 64)
                .StoreUInt(opt.CreatedAt ?? 0, 32)
                .Build();
        }

        public static IntMsgInfo Parse(CellSlice slice, bool skipPrefix = false)
        {
            CellSlice _slice = slice.Clone();
            if (!skipPrefix)
            {
                bool prefix = _slice.LoadBit();
                if (prefix) throw new ArgumentException("Invalid IntMsgInfo prefix. TLB: `int_msg_info$0`");
            }
            else
            {
                _slice.SkipBit();
            }

            bool ihrDisabled = _slice.LoadBit();
            bool bounce = _slice.LoadBit();
            bool bounced = _slice.LoadBit();
            Address? src = _slice.LoadAddress();
            Address? dest = _slice.LoadAddress();
            if (dest == null) throw new Exception("Invalid dest address s");
            Coins value = _slice.LoadCoins();
            _slice.SkipOptRef();
            Coins ihrFee = _slice.LoadCoins();
            Coins fwdFee = _slice.LoadCoins();
            ulong createdLt = (ulong)_slice.LoadUInt(64);
            uint createdAt = (uint)_slice.LoadUInt(32);

            slice.SkipBits(slice.RemainderBits - _slice.RemainderBits);
            slice.SkipRefs(slice.RemainderRefs - _slice.RemainderRefs);

            return new IntMsgInfo(new IntMsgInfoOptions
            {
                IhrDisabled = ihrDisabled,
                Bounce = bounce,
                Bounced = bounced,
                Src = src,
                Dest = dest,
                Value = value,
                IhrFee = ihrFee,
                FwdFee = fwdFee,
                CreatedLt = createdLt,
                CreatedAt = createdAt
            });
        }
    }

    public struct ExtInMsgInfoOptions
    {
        public Address? Src;
        public Address? Dest;
        public Coins? ImportFee;
    }


    public class ExtInMsgInfo : CommonMsgInfo
    {
        public ExtInMsgInfo(ExtInMsgInfoOptions opt)
        {
            _data = opt;
            _cell = new CellBuilder()
                .StoreBit(true).StoreBit(false) // ext_in_msg_info$10
                .StoreAddress(opt.Src)
                .StoreAddress(opt.Dest)
                .StoreCoins(opt.ImportFee ?? new Coins(0))
                .Build();
        }

        public static ExtInMsgInfo Parse(CellSlice slice, bool skipPrefix = false)
        {
            CellSlice _slice = slice.Clone();
            if (!skipPrefix)
            {
                byte prefix = (byte)_slice.LoadInt(2);
                if (prefix != 0b10)
                    throw new ArgumentException("Invalid ExtInMsgInfo prefix. TLB: `ext_in_msg_info$10`");
            }
            else
            {
                _slice.SkipBits(2);
            }

            Address? src = _slice.LoadAddress();
            Address? dest = _slice.LoadAddress();
            Coins importFee;
            try
            {
                importFee = _slice.LoadCoins();
            }
            catch
            {
                importFee = new Coins(0);
            }

            slice.SkipBits(slice.RemainderBits - _slice.RemainderBits);
            slice.SkipRefs(slice.RemainderRefs - _slice.RemainderRefs);

            return new ExtInMsgInfo(new ExtInMsgInfoOptions
            {
                Src = src,
                Dest = dest,
                ImportFee = importFee
            });
        }
    }

    public struct ExtOutMsgInfoOptions
    {
        public Address? Src;
        public Address? Dest;
        public ulong CreatedLt;
        public uint CreatedAt;
    }

    public class ExtOutMsgInfo : CommonMsgInfo
    {
        public ExtOutMsgInfo(ExtOutMsgInfoOptions opt)
        {
            _data = opt;
            _cell = new CellBuilder()
                .StoreBit(true).StoreBit(false) // ext_out_msg_info$11
                .StoreAddress(opt.Src)
                .StoreAddress(opt.Dest)
                .StoreUInt(opt.CreatedLt, 64)
                .StoreUInt(opt.CreatedAt, 32)
                .Build();
        }

        public static ExtOutMsgInfo Parse(CellSlice slice, bool skipPrefix = false)
        {
            CellSlice _slice = slice.Clone();
            if (!skipPrefix)
            {
                byte prefix = (byte)_slice.LoadInt(2);
                if (prefix != 0b11)
                    throw new ArgumentException("Invalid ExtOutMsgInfo prefix. TLB: `ext_out_msg_info$11`");
            }
            else
            {
                _slice.SkipBits(2);
            }

            Address? src = _slice.LoadAddress();
            //if (src == null) throw new Exception("Invalid src address");
            Address? dest = _slice.LoadAddress();
            if (dest == null)
                return new ExtOutMsgInfo(new ExtOutMsgInfoOptions
                {
                    Src = src,
                    Dest = null,
                    CreatedLt = 0,
                    CreatedAt = 0
                });
            ulong createdLt = (ulong)_slice.LoadUInt(64);
            uint createdAt = (uint)_slice.LoadUInt(32);

            slice.SkipBits(slice.RemainderBits - _slice.RemainderBits);
            slice.SkipRefs(slice.RemainderRefs - _slice.RemainderRefs);

            return new ExtOutMsgInfo(new ExtOutMsgInfoOptions
            {
                Src = src,
                Dest = dest,
                CreatedLt = createdLt,
                CreatedAt = createdAt
            });
        }
    }

    public struct MessageXOptions
    {
        public CommonMsgInfo Info;
        public StateInit? StateInit;
        public Cell? Body;
    }

    public class MessageX : BlockStruct<MessageXOptions>
    {
        public MessageX(MessageXOptions opt)
        {
            _data = opt;
            _cell = buildCell();
            Signed = false;
        }

        public bool Signed { get; private set; }

        public Cell SignedCell { get; private set; }

        Cell signCell(byte[]? privateKey = null, bool eitherSliceRef = false)
        {
            CellBuilder builder = new CellBuilder();
            byte[] body = KeyPair.Sign(_data.Body, privateKey);
            builder.StoreBytes(body);
            builder.StoreCellSlice(_data.Body.Parse());
            return builder.Build();
        }

        Cell buildCell(byte[]? privateKey = null, bool eitherSliceRef = false)
        {
            CellBuilder builder = new CellBuilder()
                .StoreCellSlice(_data.Info.Cell.Parse());
            bool maybeStateInit = _data.StateInit != null;
            if (maybeStateInit)
            {
                builder.StoreBit(true);
                builder.StoreBit(false); // Either StateInit ^StateInit
                builder.StoreCellSlice(_data.StateInit!.Cell.Parse());
            }
            else
            {
                builder.StoreBit(false);
            }

            if (_data.Body != null)
            {
                Cell body = privateKey != null
                    ? signBody(privateKey, eitherSliceRef)
                    : _data.Body!;
                bool eitherBody = _data.Body.BitsCount > builder.RemainderBits
                                  || _data.Body.RefsCount > builder.RemainderRefs;
                builder.StoreBit(eitherBody);
                if (!eitherBody)
                    try
                    {
                        builder.StoreCellSlice(body.Parse());
                    }
                    catch (Exception e)
                    {
                        builder.StoreRef(body);
                    }
                else
                    builder.StoreRef(body);
            }
            else
            {
                builder.StoreBit(false);
            }

            return builder.Build();
        }

        Cell signBody(byte[] privateKey, bool eitherSliceRef)
        {
            CellBuilder b = new CellBuilder()
                .StoreBytes(KeyPair.Sign(_data.Body, privateKey));
            if (!eitherSliceRef)
                b.StoreCellSlice(_data.Body.Parse());
            else
                b.StoreRef(_data.Body);

            return b.Build();
        }

        public MessageX Sign(byte[] privateKey, bool eitherSliceRef = false)
        {
            if (_data.Body == null) throw new Exception("MessageX body is empty");
            if (Signed) throw new Exception("MessageX already signed");
            _cell = buildCell(privateKey, eitherSliceRef);
            SignedCell = signCell(privateKey, eitherSliceRef);
            Signed = true;
            return this;
        }

        public static MessageX Parse(CellSlice slice)
        {
            CellSlice _slice = slice.Clone();
            CommonMsgInfo info = CommonMsgInfo.Parse(_slice);
            bool maybeStateInit = _slice.LoadBit();
            bool eitherStateInit = maybeStateInit && _slice.LoadBit();
            StateInit stateInit;
            try
            {
                stateInit = maybeStateInit
                    ? eitherStateInit
                        ? StateInit.Parse(_slice.LoadRef().Parse())
                        : StateInit.Parse(_slice)
                    : null;
            }
            catch (Exception e)
            {
                stateInit = null;
            }

            bool eitherBody = _slice.LoadBit();
            Cell body = eitherBody
                ? _slice.RemainderRefs > 0
                    ? _slice.LoadRef()
                    : _slice.RestoreRemainder()
                : _slice.RestoreRemainder();

            slice.SkipBits(slice.RemainderBits - _slice.RemainderBits);
            slice.SkipRefs(slice.RemainderRefs - _slice.RemainderRefs);

            return new MessageX(new MessageXOptions
            {
                Info = info,
                StateInit = stateInit,
                Body = body
            });
        }
    }

    public struct ExternalInMessageOptions
    {
        public ExtInMsgInfo Info;
        public StateInit? StateInit;
        public Cell? Body;
    }

    public class ExternalInMessage : MessageX
    {
        public ExternalInMessage(ExternalInMessageOptions opt)
            : base(new MessageXOptions { Info = opt.Info, Body = opt.Body, StateInit = opt.StateInit })
        {
        }

        public ExternalInMessage Sign(byte[] privateKey, bool eitherSliceRef = false)
        {
            return (ExternalInMessage)base.Sign(privateKey, eitherSliceRef);
        }
    }

    public struct InternalMessageOptions
    {
        public IntMsgInfo Info;
        public StateInit? StateInit;
        public Cell? Body;
    }

    public class InternalMessage : MessageX
    {
        public InternalMessage(InternalMessageOptions opt)
            : base(new MessageXOptions { Info = opt.Info, Body = opt.Body, StateInit = opt.StateInit })
        {
        }

        public InternalMessage Sign(byte[] privateKey, bool eitherSliceRef = false)
        {
            return (InternalMessage)base.Sign(privateKey, eitherSliceRef);
        }
    }

    public class OutAction : BlockStruct<object>
    {
        public static OutAction Parse(CellSlice slice)
        {
            uint prefix = (uint)slice.ReadUInt(32);
            return prefix switch
            {
                0x0ec3c86d => ActionSendMsg.Parse(slice, true),
                0xad4de08e => ActionSetCode.Parse(slice, true),
                0x36e6b809 => throw new NotImplementedException("ActionReserveCurrency"),
                0x26fa1dd4 => throw new NotImplementedException("ActionChangeLibrary"),
                _ => throw new ArgumentException("Invalid action prefix")
            };
        }
    }

    public struct ActionSendMsgOptions
    {
        public byte Mode;
        public MessageX OutMsg;
    }

    public class ActionSendMsg : OutAction
    {
        public ActionSendMsg(ActionSendMsgOptions opt)
        {
            _data = opt;
            _cell = new CellBuilder()
                .StoreUInt(0x0ec3c86d, 32)
                .StoreUInt(opt.Mode, 8)
                .StoreRef(opt.OutMsg.Cell)
                .Build();
        }

        public static ActionSendMsg Parse(CellSlice slice, bool skipPrefix = false)
        {
            BlockUtils.CheckUnderflow(slice, 40, 1);
            if (!skipPrefix)
            {
                BigInteger prefix = slice.LoadUInt(32);
                if (prefix != 0x0ec3c86d) throw new ArgumentException("Invalid action prefix");
            }
            else
            {
                slice.SkipBits(32);
            }

            return new ActionSendMsg(new ActionSendMsgOptions
            {
                Mode = (byte)slice.LoadUInt(8),
                OutMsg = MessageX.Parse(slice.LoadRef().Parse())
            });
        }
    }

    public struct ActionSetCodeOptions
    {
        public Cell NewCode;
    }

    public class ActionSetCode : OutAction
    {
        public ActionSetCode(ActionSetCodeOptions opt)
        {
            _data = opt;
            _cell = new CellBuilder()
                .StoreUInt(0xad4de08e, 32)
                .StoreRef(opt.NewCode)
                .Build();
        }

        public static ActionSetCode Parse(CellSlice slice, bool skipPrefix = false)
        {
            BlockUtils.CheckUnderflow(slice, 32, 1);
            if (!skipPrefix)
            {
                BigInteger prefix = slice.LoadUInt(32);
                if (prefix != 0xad4de08e) throw new ArgumentException("Invalid action prefix");
            }
            else
            {
                slice.SkipBits(32);
            }

            return new ActionSetCode(new ActionSetCodeOptions { NewCode = slice.LoadRef() });
        }
    }

    public struct OutListOptions
    {
        public OutAction[] Actions;
    }


    public class OutList : BlockStruct<OutListOptions>
    {
        public OutList(OutListOptions opt)
        {
            if (opt.Actions.Length > 255)
                throw new ArgumentException("Too many actions. May be from 0 to 255 (includes)");
            _data = opt;
            _cell = buildCell();
        }

        /*
        out_list_empty$_ = OutList 0;
        out_list$_ {n:#} prev:^(OutList n) action:OutAction
          = OutList (n + 1);
        */
        Cell buildCell()
        {
            Cell actionList = new Cell(new Bits(0), Array.Empty<Cell>());

            foreach (OutAction action in _data.Actions)
                actionList = new CellBuilder()
                    .StoreRef(actionList)
                    .StoreCellSlice(action.Cell.Parse())
                    .Build();

            return actionList;
        }

        public OutList Parse(CellSlice slice)
        {
            CellSlice _slice = slice.Clone();
            List<OutAction> actions = new List<OutAction>();
            while (_slice.RemainderRefs > 0)
            {
                Cell prev = _slice.LoadRef();
                actions.Add(OutAction.Parse(_slice));
                _slice = prev.Parse();
            }

            return new OutList(new OutListOptions { Actions = actions.ToArray() });
        }
    }
}