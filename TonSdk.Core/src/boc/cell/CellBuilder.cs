using System;

namespace TonSdk.Core.Boc;

public class CellBuilder(int length = 1023) : BitsBuilderImpl<CellBuilder, Cell>(length)
{
    readonly Cell[] refs = new Cell[CellTraits.max_refs];

    int refEn;

    public int RemainderRefs => CellTraits.max_refs - refEn;

    public Cell[] Refs => refs.Length == refEn ? refs : refs.Slice(0, refEn);

    void CheckRefsOverflow(ref Cell[] refs)
    {
        if (refEn + refs.Length > CellTraits.max_refs) throw new ArgumentException("CellBuilder refs overflow");
    }

    void CheckRefsOverflow(int refsCnt)
    {
        if (refEn + refsCnt > CellTraits.max_refs) throw new ArgumentException("CellBuilder refs overflow");
    }

    public CellBuilder StoreRefs(ref Cell[] refs, bool needCheck = true)
    {
        if (needCheck) CheckRefsOverflow(ref refs);

        foreach (Cell cell in refs) this.refs[refEn++] = cell;

        return this;
    }

    public CellBuilder StoreRef(Cell cell, bool needCheck = true)
    {
        if (needCheck) CheckRefsOverflow(1);

        refs[refEn++] = cell;

        return this;
    }

    public CellBuilder StoreCellSlice(CellSlice bs, bool needCheck = true)
    {
        if (needCheck)
        {
            CheckBitsOverflow(bs.RemainderBits);
            CheckRefsOverflow(bs.RemainderRefs);
        }

        StoreBits(bs.Bits, false);
        Cell[] r = bs.Refs;
        return StoreRefs(ref r, false);
    }

    public CellBuilder StoreOptRef(Cell? cell, bool needCheck = true)
    {
        bool opt = cell != null;
        if (needCheck)
        {
            CheckBitsOverflow(1);
            if (opt) CheckRefsOverflow(1);
        }

        if (opt) StoreRef(cell!, false);
        return StoreBit(opt, false);
    }

    public CellBuilder StoreDict<TK, TV>(HashmapE<TK, TV> hashmap, bool needCheck = true)
    {
        return StoreCellSlice(hashmap.Build().Parse(), needCheck);
    }

    public override Cell Build()
    {
        return new Cell(Data, Refs);
    }

    public override CellBuilder Clone()
    {
        throw new NotImplementedException();
    }
}