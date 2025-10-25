using System;
using System.Collections.Generic;
using System.Numerics;
using TonSdk.Core.Addresses;
using TonSdk.Core.boc;
using TonSdk.Core.boc.bits;
using TonSdk.Core.boc.Cells;
using TonSdk.Core.Cryptography;
using TonSdk.Core.Economics;

namespace TonSdk.Core.Blocks;

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
    public T Data { get; protected set; }
    public Cell Cell { get; protected set; }
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
        Data = opt;
        Cell = new CellBuilder()
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
        Data = opt;
        Cell = new CellBuilder()
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


        Data = opt;
        CellBuilder builder = new();

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
        Cell = builder.Build();
    }

    public static StateInit Parse(CellSlice sliceOrig)
    {
        CellSlice slice = sliceOrig.Clone();

        bool maybeSplitDepth = slice.LoadBit();
        byte? splitDepth = maybeSplitDepth ? (byte?)slice.LoadUInt(5) : null;

        bool maybeSpecial = slice.LoadBit();

        TickTock? special = maybeSpecial ? TickTock.Parse(slice) : null;

        Cell? code = slice.LoadOptRef();
        Cell? data = slice.LoadOptRef();
        HashmapE<BigInteger, SimpleLib> library = slice.LoadDict(new HashmapOptions<BigInteger, SimpleLib>
        {
            KeySize = 256,
            Deserializers = new HashmapDeserializers<BigInteger, SimpleLib>
            {
                Key = kBits => kBits.Parse().LoadUInt(256),
                Value = vCell => SimpleLib.Parse(vCell.Parse())
            }
        });

        slice.SkipBits(slice.RemainderBits - slice.RemainderBits);
        slice.SkipRefs(slice.RemainderRefs - slice.RemainderRefs);

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
    public static CommonMsgInfo Parse(CellSlice sliceOrig)
    {
        CellSlice slice = sliceOrig.Clone();
        if (!slice.LoadBit()) return IntMsgInfo.Parse(slice, true);
        if (!slice.LoadBit()) return ExtInMsgInfo.Parse(slice, true);
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
        Data = opt;
        Cell = new CellBuilder()
            .StoreBit(false) // int_msg_info$0
            .StoreBit(opt.IhrDisabled ?? true)
            .StoreBit(opt.Bounce)
            .StoreBit(opt.Bounced ?? false)
            .StoreAddress(opt.Src)
            .StoreAddress(opt.Dest)
            .StoreCoins(opt.Value)
            .StoreBit(false) // TODO: implement extracurrency collection
            .StoreCoins(opt.IhrFee ?? Coins.Zero)
            .StoreCoins(opt.FwdFee ?? Coins.Zero)
            .StoreUInt(opt.CreatedLt ?? 0, 64)
            .StoreUInt(opt.CreatedAt ?? 0, 32)
            .Build();
    }

    public static IntMsgInfo Parse(CellSlice sliceOrig, bool skipPrefix = false)
    {
        CellSlice slice = sliceOrig.Clone();
        if (!skipPrefix)
        {
            bool prefix = slice.LoadBit();
            if (prefix) throw new ArgumentException("Invalid IntMsgInfo prefix. TLB: `int_msg_info$0`");
        }
        else
        {
            slice.SkipBit();
        }

        bool ihrDisabled = slice.LoadBit();
        bool bounce = slice.LoadBit();
        bool bounced = slice.LoadBit();
        Address? src = slice.LoadAddress();
        Address? dest = slice.LoadAddress();
        if (dest == null) throw new Exception("Invalid dest address s");
        Coins value = slice.LoadCoins();
        slice.SkipOptRef();
        Coins ihrFee = slice.LoadCoins();
        Coins fwdFee = slice.LoadCoins();
        ulong createdLt = (ulong)slice.LoadUInt(64);
        uint createdAt = (uint)slice.LoadUInt(32);

        slice.SkipBits(slice.RemainderBits - slice.RemainderBits);
        slice.SkipRefs(slice.RemainderRefs - slice.RemainderRefs);

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
        Data = opt;
        Cell = new CellBuilder()
            .StoreBit(true).StoreBit(false) // ext_in_msg_info$10
            .StoreAddress(opt.Src)
            .StoreAddress(opt.Dest)
            .StoreCoins(opt.ImportFee ?? Coins.Zero)
            .Build();
    }

    public static ExtInMsgInfo Parse(CellSlice sliceOrig, bool skipPrefix = false)
    {
        CellSlice slice = sliceOrig.Clone();
        if (!skipPrefix)
        {
            byte prefix = (byte)slice.LoadInt(2);
            if (prefix != 0b10)
                throw new ArgumentException("Invalid ExtInMsgInfo prefix. TLB: `ext_in_msg_info$10`");
        }
        else
        {
            slice.SkipBits(2);
        }

        Address? src = slice.LoadAddress();
        Address? dest = slice.LoadAddress();
        Coins importFee;
        try
        {
            importFee = slice.LoadCoins();
        }
        catch
        {
            importFee = Coins.Zero;
        }

        slice.SkipBits(slice.RemainderBits - slice.RemainderBits);
        slice.SkipRefs(slice.RemainderRefs - slice.RemainderRefs);

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
        Data = opt;
        Cell = new CellBuilder()
            .StoreBit(true).StoreBit(false) // ext_out_msg_info$11
            .StoreAddress(opt.Src)
            .StoreAddress(opt.Dest)
            .StoreUInt(opt.CreatedLt, 64)
            .StoreUInt(opt.CreatedAt, 32)
            .Build();
    }

    public static ExtOutMsgInfo Parse(CellSlice sliceOrig, bool skipPrefix = false)
    {
        CellSlice slice = sliceOrig.Clone();
        if (!skipPrefix)
        {
            byte prefix = (byte)slice.LoadInt(2);
            if (prefix != 0b11)
                throw new ArgumentException("Invalid ExtOutMsgInfo prefix. TLB: `ext_out_msg_info$11`");
        }
        else
        {
            slice.SkipBits(2);
        }

        Address? src = slice.LoadAddress();
        //if (src == null) throw new Exception("Invalid src address");
        Address? dest = slice.LoadAddress();
        if (dest == null)
            return new ExtOutMsgInfo(new ExtOutMsgInfoOptions
            {
                Src = src,
                Dest = null,
                CreatedLt = 0,
                CreatedAt = 0
            });
        ulong createdLt = (ulong)slice.LoadUInt(64);
        uint createdAt = (uint)slice.LoadUInt(32);

        slice.SkipBits(slice.RemainderBits - slice.RemainderBits);
        slice.SkipRefs(slice.RemainderRefs - slice.RemainderRefs);

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
        Data = opt;
        Cell = BuildCell();
        Signed = false;
    }

    public bool Signed { get; private set; }

    public Cell SignedCell { get; private set; }

    Cell SignCell(byte[]? privateKey = null, bool eitherSliceRef = false)
    {
        CellBuilder builder = new();
        byte[] body = KeyPair.Sign(Data.Body, privateKey);
        builder.StoreBytes(body);
        builder.StoreCellSlice(Data.Body.Parse());
        return builder.Build();
    }

    Cell BuildCell(byte[]? privateKey = null, bool eitherSliceRef = false)
    {
        CellBuilder builder = new CellBuilder()
            .StoreCellSlice(Data.Info.Cell.Parse());
        bool maybeStateInit = Data.StateInit != null;
        if (maybeStateInit)
        {
            builder.StoreBit(true);
            builder.StoreBit(false); // Either StateInit ^StateInit
            builder.StoreCellSlice(Data.StateInit!.Cell.Parse());
        }
        else
        {
            builder.StoreBit(false);
        }

        if (Data.Body != null)
        {
            Cell body = privateKey != null
                ? SignBody(privateKey, eitherSliceRef)
                : Data.Body!;
            bool eitherBody = Data.Body.BitsCount > builder.RemainderBits
                              || Data.Body.RefsCount > builder.RemainderRefs;
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

    Cell SignBody(byte[] privateKey, bool eitherSliceRef)
    {
        CellBuilder b = new CellBuilder()
            .StoreBytes(KeyPair.Sign(Data.Body, privateKey));
        if (!eitherSliceRef)
            b.StoreCellSlice(Data.Body.Parse());
        else
            b.StoreRef(Data.Body);

        return b.Build();
    }

    public MessageX Sign(byte[] privateKey, bool eitherSliceRef = false)
    {
        if (Data.Body == null) throw new Exception("MessageX body is empty");
        if (Signed) throw new Exception("MessageX already signed");
        Cell = BuildCell(privateKey, eitherSliceRef);
        SignedCell = SignCell(privateKey, eitherSliceRef);
        Signed = true;
        return this;
    }

    public static MessageX Parse(CellSlice sliceOrig)
    {
        CellSlice slice = sliceOrig.Clone();
        CommonMsgInfo info = CommonMsgInfo.Parse(slice);
        bool maybeStateInit = slice.LoadBit();
        bool eitherStateInit = maybeStateInit && slice.LoadBit();
        StateInit stateInit;
        try
        {
            stateInit = maybeStateInit
                ? eitherStateInit
                    ? StateInit.Parse(slice.LoadRef().Parse())
                    : StateInit.Parse(slice)
                : null;
        }
        catch (Exception e)
        {
            stateInit = null;
        }

        bool eitherBody = slice.LoadBit();
        Cell body = eitherBody
            ? slice.RemainderRefs > 0
                ? slice.LoadRef()
                : slice.RestoreRemainder()
            : slice.RestoreRemainder();

        slice.SkipBits(slice.RemainderBits - slice.RemainderBits);
        slice.SkipRefs(slice.RemainderRefs - slice.RemainderRefs);

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

public class ExternalInMessage(ExternalInMessageOptions opt) : MessageX(new MessageXOptions
    { Info = opt.Info, Body = opt.Body, StateInit = opt.StateInit })
{
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

public class InternalMessage(InternalMessageOptions opt) : MessageX(new MessageXOptions
    { Info = opt.Info, Body = opt.Body, StateInit = opt.StateInit })
{
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
        Data = opt;
        Cell = new CellBuilder()
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
        Data = opt;
        Cell = new CellBuilder()
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
        Data = opt;
        Cell = BuildCell();
    }

    /*
    out_list_empty$_ = OutList 0;
    out_list$_ {n:#} prev:^(OutList n) action:OutAction
      = OutList (n + 1);
    */
    Cell BuildCell()
    {
        Cell actionList = new(new Bits(0), Array.Empty<Cell>());

        foreach (OutAction action in Data.Actions)
            actionList = new CellBuilder()
                .StoreRef(actionList)
                .StoreCellSlice(action.Cell.Parse())
                .Build();

        return actionList;
    }

    public OutList Parse(CellSlice sliceOrig)
    {
        CellSlice slice = sliceOrig.Clone();
        List<OutAction> actions = new();
        while (slice.RemainderRefs > 0)
        {
            Cell prev = slice.LoadRef();
            actions.Add(OutAction.Parse(slice));
            slice = prev.Parse();
        }

        return new OutList(new OutListOptions { Actions = actions.ToArray() });
    }
}