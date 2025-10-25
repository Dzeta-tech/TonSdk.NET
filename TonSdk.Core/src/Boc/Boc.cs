using System;
using System.Collections.Generic;
using System.Linq;
using JustCRC32C;
using TonSdk.Core.Boc.bits;
using TonSdk.Core.Boc.Cells;

namespace TonSdk.Core.Boc;

public static class BagOfCells
{
    const uint BocConstructor = 0xb5ee9c72;

    static BocHeader DeserializeHeader(Bits headerBits)
    {
        BitsSlice hs = headerBits.Parse();
        if ((uint)hs.LoadUInt(32) != BocConstructor) throw new Exception("Unknown BOC constructor");

        bool hasIdx = hs.LoadBit();
        bool hasCrc32C = hs.LoadBit();
        bool hasCacheBits = hs.LoadBit();
        byte flags = (byte)hs.LoadUInt(2);
        if (flags != 0) throw new Exception("Unknown flags");
        byte sizeBytes = (byte)hs.LoadUInt(3);
        if (sizeBytes > 4) throw new Exception("Invalid size");
        byte offsetBytes = (byte)hs.LoadUInt(8);
        if (offsetBytes > 8) throw new Exception("Invalid offset");
        uint cellsNum = (uint)hs.LoadUInt(sizeBytes * 8);
        uint rootsNum = (uint)hs.LoadUInt(sizeBytes * 8);
        if (rootsNum < 1) throw new Exception("Invalid rootsNum");
        uint absentNum = (uint)hs.LoadUInt(sizeBytes * 8);
        if (rootsNum + absentNum > cellsNum) throw new Exception("Invalid absentNum");
        ulong totalCellsSize = (ulong)hs.LoadUInt(offsetBytes * 8);

        ulong calcRemainderBits = rootsNum * sizeBytes * 8
                                  + totalCellsSize * 8
                                  + (hasIdx ? cellsNum * offsetBytes * 8 : 0)
                                  + (ulong)(hasCrc32C ? 32 : 0);

        if ((ulong)hs.RemainderBits != calcRemainderBits) throw new Exception("Invalid BOC size");

        uint[] rootList = new uint[rootsNum];
        for (int i = 0; i < rootsNum; i++) rootList[i] = (uint)hs.LoadUInt(sizeBytes * 8);

        if (hasIdx) hs.SkipBits((int)(cellsNum * offsetBytes * 8));

        Bits cellsData = hs.LoadBits((int)(totalCellsSize * 8));

        if (hasCrc32C)
        {
            Bits crc32Bits = headerBits.Slice(0, -32);
            //Console.WriteLine(hs.RemainderBits);
            uint crc32C = (uint)hs.LoadUInt32Le();
            uint crc32CCalc = Crc32C.Calculate(crc32Bits.ToBytes());
            //Console.WriteLine(crc32c);
            //Console.WriteLine(crc32c_calc);
            if (crc32C != crc32CCalc) throw new Exception("Invalid CRC32C");
        }

        return new BocHeader
        {
            HasIdx = hasIdx,
            HasCrc32C = hasCrc32C,
            HasCacheBits = hasCacheBits,
            Flags = flags,
            SizeBytes = sizeBytes,
            OffsetBytes = offsetBytes,
            CellsNum = cellsNum,
            RootsNum = rootsNum,
            AbsentNum = absentNum,
            TotalCellsSize = totalCellsSize,
            RootList = rootList,
            CellsData = cellsData
        };
    }

    static RawCell DeserializeCell(BitsSlice dataSlice, ushort refIndexSize)
    {
        if (dataSlice.RemainderBits < 2) throw new Exception("BOC not enough bytes to encode cell descriptors");

        uint refsDescriptor = (uint)dataSlice.LoadUInt(8);
        uint level = refsDescriptor >> 5;
        uint totalRefs = refsDescriptor & 7;
        bool hasHashes = (refsDescriptor & 16) != 0;
        bool isExotic = (refsDescriptor & 8) != 0;
        bool isAbsent = totalRefs == 7 && hasHashes;

        if (isAbsent) throw new Exception("BoC can't deserialize absent cell");

        if (totalRefs > 4) throw new Exception($"BoC cell can't has more than 4 refs {totalRefs}");

        uint bitsDescriptor = (uint)dataSlice.LoadUInt(8);
        bool isAugmented = (bitsDescriptor & 1) != 0;
        long dataSize = (bitsDescriptor >> 1) + (isAugmented ? 1 : 0);
        uint hashesSize = hasHashes ? (level + 1) * 32 : 0;
        uint depthSize = hasHashes ? (level + 1) * 2 : 0;

        if (dataSlice.RemainderBits < hashesSize + depthSize + dataSize + refIndexSize * totalRefs)
            throw new Exception("BoC not enough bytes to encode cell data");

        if (hasHashes) dataSlice.SkipBits((int)(hashesSize + depthSize));

        Bits data = isAugmented
            ? dataSlice.LoadBits((int)dataSize * 8).Rollback()
            : dataSlice.LoadBits((int)dataSize * 8);

        if (isExotic && data.Length < 8) throw new Exception("BoC not enough bytes for an exotic cell type");

        CellType type = isExotic ? (CellType)(int)data.Slice(0, 8).Parse().LoadInt(8) : CellType.Ordinary;

        if (isExotic && type == CellType.Ordinary)
            throw new Exception("BoC an exotic cell can't be of ordinary type");

        ulong[] refs = new ulong[totalRefs];
        for (int i = 0; i < totalRefs; i++) refs[i] = (ulong)dataSlice.LoadUInt(refIndexSize * 8);

        return new RawCell
        {
            Type = type,
            Builder = new CellBuilder(data.Length).StoreBits(data),
            Refs = refs
        };
    }

    public static Cell[] DeserializeBoc(Bits data)
    {
        BocHeader headers = DeserializeHeader(data);
        RawCell[] rawCells = new RawCell[headers.CellsNum];

        BitsSlice cellsDataSlice = headers.CellsData.Parse();

        for (int i = 0; i < headers.CellsNum; i++) rawCells[i] = DeserializeCell(cellsDataSlice, headers.SizeBytes);

        for (int i = (int)(headers.CellsNum - 1); i >= 0; i--)
        {
            foreach (ulong refIndex in rawCells[i].Refs)
            {
                if (refIndex >= (ulong)rawCells.Length)
                    throw new Exception(
                        $"BOC deserialization error: Reference index {refIndex} is out of bounds (total cells: {rawCells.Length})");

                RawCell rawRefCell = rawCells[refIndex];
                if (refIndex < (ulong)i) throw new Exception("Topological order is broken");

                rawCells[i].Builder.StoreRef(rawRefCell.Builder.Build());
            }

            rawCells[i].Cell = rawCells[i].Builder.Build();
        }

        return headers.RootList.Select(i => rawCells[i].Cell!).ToArray();
    }


    static Bits SerializeCell(Cell cell, Dictionary<Bits, int> cellsIndex, int refSize)
    {
        Bits ret = cell.BitsWithDescriptors;
        refSize *= 8;
        int l = ret.Length + cell.RefsCount * refSize;
        BitsBuilder b = new BitsBuilder(l).StoreBits(ret);
        foreach (Cell refCell in cell.Refs)
        {
            Bits refHash = refCell.Hash;
            int refIndex = cellsIndex[refHash];
            b.StoreUInt(refIndex, refSize);
        }

        return b.Build();
    }


    public static Bits SerializeBoc(
        Cell root,
        bool hasIdx = false,
        bool hasCrc32C = true
    )
    {
        return SerializeBoc(new[] { root }, hasIdx, hasCrc32C);
    }


    static (List<(Bits, Cell)> sortedCells, Dictionary<Bits, int> hashToIndex)
        TopologicalSort(Cell[] roots)
    {
        // List of already sorted vertices of the graph
        List<(Bits, Cell)> sortedCells = new();
        // Dictionary that maps the cell hash to its index in the sorted list
        Dictionary<Bits, int> hashToIndex = new(new BitsEqualityComparer());

        // Recursive function for graph traversal and topological sorting
        void visitCell(Cell cell)
        {
            foreach (Cell neighbor in cell.Refs)
                if (!hashToIndex.ContainsKey(neighbor.Hash))
                    visitCell(neighbor);

            // Check that the cell is not yet added to the list of sorted cells
            if (!hashToIndex.ContainsKey(cell.Hash))
            {
                // Add the cell to the beginning of the list of sorted cells
                sortedCells.Insert(0, (cell.Hash, cell));
                // Shift the already added cells one position to the right
                for (int i = 1; i < sortedCells.Count; i++) hashToIndex[sortedCells[i].Item2.Hash]++;

                // Add the cell to the hashToIndex dictionary
                hashToIndex[cell.Hash] = 0;
            }
        }

        // Perform traversal and topological sorting for each vertex of the graph
        for (int i = roots.Length - 1; i > -1; i--)
        {
            // foreach (var rootCell in roots) {
            Cell rootCell = roots[i];
            foreach (Cell cell in rootCell.Refs) visitCell(cell);

            visitCell(rootCell);
        }

        return (sortedCells, hashToIndex);
    }


    public static Bits SerializeBoc(
        Cell[] roots,
        bool hasIdx = false,
        bool hasCrc32C = true
        // bool hasCacheBits = false    // always false
        // uint flags = 0               // always 0
    )
    {
        const bool hasCacheBits = false;
        const uint flags = 0;
        (List<(Bits, Cell)> sortedCells, Dictionary<Bits, int> indexHashmap) = TopologicalSort(roots);

        int cellsNum = sortedCells.Count;
        int sBytes = (cellsNum.BitLength() + 7) / 8;

        int[] offsets = new int[cellsNum];
        int totalSize = 0;
        BitsBuilder dataBuilder = new(cellsNum * (16 + 1024 + sBytes * 8 * 4));
        for (int i = 0; i < cellsNum; i++)
        {
            Bits serializedCell = SerializeCell(sortedCells[i].Item2, indexHashmap, sBytes);
            dataBuilder.StoreBits(serializedCell);
            totalSize += serializedCell.Length / 8;
            offsets[i] = totalSize;
        }

        Bits dataBits = dataBuilder.Build();
        int offsetBytes = Math.Max((dataBits.Length.BitLength() + 7) / 8, 1);

        /*
          serialized_boc#b5ee9c72 has_idx:(## 1) has_crc32c:(## 1)
                                  has_cache_bits:(## 1) flags:(## 2) { flags = 0 }
                                  size:(## 3) { size <= 4 }
                                  off_bytes:(## 8) { off_bytes <= 8 }
                                  cells:(##(size * 8))
                                  roots:(##(size * 8)) { roots >= 1 }
                                  absent:(##(size * 8)) { roots + absent <= cells }
                                  tot_cells_size:(##(off_bytes * 8))
                                  root_list:(roots * ##(size * 8))
                                  index:has_idx?(cells * ##(off_bytes * 8))
                                  cell_data:(tot_cells_size * [ uint8 ])
                                  crc32c:has_crc32c?uint32
                                  = BagOfCells;
         */
        int l = 32 + 1 + 1 + 1 + 2 + 3 + 8 + sBytes * 8 + sBytes * 8 + sBytes * 8 + offsetBytes * 8 +
                roots.Length * sBytes * 8 + (hasIdx ? 1 : 0) * cellsNum * offsetBytes * 8 + dataBits.Length +
                (hasCrc32C ? 1 : 0) * 32;
        BitsBuilder bocBuilder = new BitsBuilder(l)
            .StoreUInt(BocConstructor, 32, false) // serialized_boc#b5ee9c72
            .StoreBit(hasIdx, false) // has_idx:(## 1)
            .StoreBit(hasCrc32C, false) // has_crc32c:(## 1)
            .StoreBit(hasCacheBits, false) // has_cache_bits:(## 1)
            .StoreUInt(flags, 2, false) // flags:(## 2) { flags = 0 }
            .StoreUInt(sBytes, 3, false) // size:(## 3) { size <= 4 }
            .StoreUInt(offsetBytes, 8, false) // off_bytes:(## 8) { off_bytes <= 8 }
            .StoreUInt(cellsNum, sBytes * 8, false) // cells:(##(size * 8))
            .StoreUInt(roots.Length, sBytes * 8) // roots:(##(size * 8)) { roots >= 1 }
            .StoreUInt(0, sBytes * 8, false) // ??? absent:(##(size * 8)) { roots + absent <= cells }
            .StoreUInt(dataBits.Length / 8, offsetBytes * 8, false); // tot_cells_size:(##(off_bytes * 8))

        foreach (Cell rootCell in roots)
            bocBuilder.StoreUInt(indexHashmap[rootCell.Hash], sBytes * 8,
                false); // root_list:(roots * ##(size * 8))

        if (hasIdx)
            foreach (int offset in offsets)
                bocBuilder.StoreUInt(offset, offsetBytes * 8, false); // index:has_idx?(cells * ##(off_bytes * 8))

        bocBuilder.StoreBits(dataBits, false); // cell_data:(tot_cells_size * [ uint8 ])

        if (hasCrc32C)
        {
            uint crc32C = Crc32C.Calculate(bocBuilder.Build().Augment().ToBytes());
            bocBuilder.StoreUInt32Le(crc32C); // crc32c:has_crc32c?uint32
        }

        return bocBuilder.Build();
    }

    struct BocHeader
    {
        public bool HasIdx;
        public bool HasCrc32C;
        public bool HasCacheBits;
        public byte Flags;
        public byte SizeBytes;
        public byte OffsetBytes;
        public uint CellsNum;
        public uint RootsNum;
        public uint AbsentNum;
        public ulong TotalCellsSize;
        public uint[] RootList;
        public Bits CellsData;
    }

    struct RawCell
    {
        public Cell? Cell;
        public CellType Type;
        public CellBuilder Builder;
        public ulong[] Refs;
    }
}