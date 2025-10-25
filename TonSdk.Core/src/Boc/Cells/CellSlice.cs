using System;
using TonSdk.Core.boc.bits;

namespace TonSdk.Core.boc.Cells;

public class CellSlice : BitsSliceImpl<CellSlice, Cell>
{
    readonly Cell cell;
    readonly int refsEn;

    int refsSt;

    public CellSlice(Cell cell) : base(cell.Bits)
    {
        this.cell = cell;
        refsEn = cell.RefsCount;
    }

    public CellSlice(Cell cell, int bitsSt, int bitsEn, int refsSt, int refsEn) : base(cell.Bits, bitsSt,
        bitsEn)
    {
        this.cell = cell;
        this.refsSt = refsSt;
        this.refsEn = refsEn;
    }

    public int RemainderRefs => refsEn - refsSt;

    public Cell[] Refs => cell.Refs.Slice(refsSt, refsEn);

    void CheckRefsUnderflow(int refEnd)
    {
        if (refEnd > refsEn) throw new ArgumentException("CellSlice refs underflow");
    }

    public CellSlice SkipRefs(int size)
    {
        int refEnd = refsSt + size;
        CheckRefsUnderflow(refEnd);
        refsSt = refEnd;
        return this;
    }

    public Cell[] ReadRefs(int size)
    {
        int refEnd = refsSt + size;
        CheckRefsUnderflow(refEnd);
        return cell.Refs.Slice(refsSt, refEnd);
    }

    public Cell[] LoadRefs(int size)
    {
        int refEnd = refsSt + size;
        CheckRefsUnderflow(refEnd);
        Cell[] refs = cell.Refs.Slice(refsSt, refEnd);
        refsSt = refEnd;
        return refs;
    }

    public CellSlice SkipRef()
    {
        return SkipRefs(1);
    }

    public Cell ReadRef()
    {
        int refEnd = refsSt + 1;
        CheckRefsUnderflow(refEnd);
        return cell.Refs[refsSt];
    }

    public Cell LoadRef()
    {
        int refEnd = refsSt + 1;
        CheckRefsUnderflow(refEnd);
        Cell @ref = cell.Refs[refsSt];
        refsSt = refEnd;
        return @ref;
    }

    public CellSlice SkipOptRef()
    {
        Cell? optRef = ReadOptRef();

        if (optRef != null)
        {
            SkipBit();
            SkipRef();
        }
        else
        {
            SkipBit();
        }

        return this;
    }

    public Cell? ReadOptRef()
    {
        bool opt = ReadBit();
        return opt ? ReadRef() : null;
    }

    public Cell? LoadOptRef()
    {
        Cell? optRef = ReadOptRef();
        SkipBit();
        if (optRef != null) SkipRef();
        return optRef;
    }

    public HashmapE<TK, TV> ReadDict<TK, TV>(HashmapOptions<TK, TV> opt)
    {
        return HashmapE<TK, TV>.Deserialize(this, opt, false);
    }

    public HashmapE<TK, TV> LoadDict<TK, TV>(HashmapOptions<TK, TV> opt)
    {
        return HashmapE<TK, TV>.Deserialize(this, opt);
    }

    public Cell RestoreRemainder()
    {
        return new Cell(Bits, Refs);
    }

    public override Cell Restore()
    {
        return cell;
    }

    public CellSlice Clone()
    {
        return new CellSlice(cell, BitsSt, BitsEn, refsSt, refsEn);
    }
}