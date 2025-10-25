using System;

namespace TonSdk.Core.Boc
{
    public class CellSlice : BitsSliceImpl<CellSlice, Cell>
    {
        readonly Cell _cell;
        readonly int _refs_en;

        int _refs_st;

        public CellSlice(Cell cell) : base(cell.Bits)
        {
            _cell = cell;
            _refs_en = cell.RefsCount;
        }

        public CellSlice(Cell cell, int bits_st, int bits_en, int refs_st, int refs_en) : base(cell.Bits, bits_st,
            bits_en)
        {
            _cell = cell;
            _refs_st = refs_st;
            _refs_en = refs_en;
        }

        public int RemainderRefs => _refs_en - _refs_st;

        public Cell[] Refs => _cell.Refs.slice(_refs_st, _refs_en);

        void CheckRefsUnderflow(int refEnd)
        {
            if (refEnd > _refs_en) throw new ArgumentException("CellSlice refs underflow");
        }

        public CellSlice SkipRefs(int size)
        {
            int refEnd = _refs_st + size;
            CheckRefsUnderflow(refEnd);
            _refs_st = refEnd;
            return this;
        }

        public Cell[] ReadRefs(int size)
        {
            int refEnd = _refs_st + size;
            CheckRefsUnderflow(refEnd);
            return _cell.Refs.slice(_refs_st, refEnd);
        }

        public Cell[] LoadRefs(int size)
        {
            int refEnd = _refs_st + size;
            CheckRefsUnderflow(refEnd);
            Cell[] refs = _cell.Refs.slice(_refs_st, refEnd);
            _refs_st = refEnd;
            return refs;
        }

        public CellSlice SkipRef()
        {
            return SkipRefs(1);
        }

        public Cell ReadRef()
        {
            int refEnd = _refs_st + 1;
            CheckRefsUnderflow(refEnd);
            return _cell.Refs[_refs_st];
        }

        public Cell LoadRef()
        {
            int refEnd = _refs_st + 1;
            CheckRefsUnderflow(refEnd);
            Cell _ref = _cell.Refs[_refs_st];
            _refs_st = refEnd;
            return _ref;
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

        public HashmapE<K, V> ReadDict<K, V>(HashmapOptions<K, V> opt)
        {
            return HashmapE<K, V>.Deserialize(this, opt, false);
        }

        public HashmapE<K, V> LoadDict<K, V>(HashmapOptions<K, V> opt)
        {
            return HashmapE<K, V>.Deserialize(this, opt);
        }

        public Cell RestoreRemainder()
        {
            return new Cell(Bits, Refs);
        }

        public override Cell Restore()
        {
            return _cell;
        }

        public CellSlice Clone()
        {
            return new CellSlice(_cell, _bits_st, _bits_en, _refs_st, _refs_en);
        }
    }
}