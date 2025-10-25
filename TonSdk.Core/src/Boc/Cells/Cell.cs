using System;
using System.Collections.Generic;
using System.Linq;
using TonSdk.Core.boc.bits;

namespace TonSdk.Core.boc.Cells;

public class Cell
{
    public readonly Bits Bits;
    public readonly int BitsCount;
    public readonly int Depth;
    public readonly int FullData;

    public readonly bool IsExotic;
    public readonly Cell[] Refs;
    public readonly int RefsCount;
    public readonly CellType Type;
    Bits? bitsWithDescriptors;
    Bits? hash;


    public Cell(Bits bits, Cell[] refs, CellType type = CellType.Ordinary)
    {
        if (bits.Length > CellTraits.max_bits)
            throw new ArgumentException($"Bits should have at most {CellTraits.max_bits} bits.", nameof(Bits));

        if (refs.Length > CellTraits.max_refs)
            throw new ArgumentException($"Refs should have at most {CellTraits.max_refs} elements.", nameof(Refs));

        Bits = bits;
        Refs = refs;
        Type = type;
        RefsCount = refs.Length;
        BitsCount = bits.Length;
        FullData = (bits.Length + 7) / 8 + bits.Length / 8;
        IsExotic = Type != CellType.Ordinary;
        Depth = RefsCount == 0 ? 0 : refs.Max(cell => cell.Depth) + 1;
    }

    public Cell(string bitString, params Cell[] refs) :
        this(new Bits(bitString), refs)
    {
    }

    public Bits BitsWithDescriptors =>
        bitsWithDescriptors == null ? BuildBitsWithDescriptors() : bitsWithDescriptors;

    public Bits Hash => hash ?? CalcHash();

    public static Cell From(string bitsString)
    {
        return From(new Bits(bitsString));
    }

    public static Cell From(Bits bits)
    {
        return BagOfCells.DeserializeBoc(bits)[0];
    }

    string ToFiftHex(ushort indent = 1, int size = 0)
    {
        List<string> output = new() { string.Concat(Enumerable.Repeat(" ", indent * size)) + Bits.ToString("fiftHex") };
        output.AddRange(Refs.Select(cell => $"\n{cell.ToFiftHex(indent, size + 1)}"));
        return string.Join("", output);
    }

    string ToFiftBin(ushort indent = 1, int size = 0)
    {
        List<string> output = new() { string.Concat(Enumerable.Repeat(" ", indent * size)) + Bits.ToString("fiftBin") };

        foreach (Cell cell in Refs) output.Add($"\n{cell.ToFiftBin(indent, size + 1)}");

        return string.Join("", output);
    }

    public CellSlice Parse()
    {
        return new CellSlice(this);
    }

    public override string ToString()
    {
        return ToString("base64");
    }

    public string ToString(string mode)
    {
        return mode switch
        {
            "hex" => Serialize().ToString("hex"),
            "fiftBin" => ToFiftBin(),
            "fiftHex" => ToFiftHex(),
            "base64" => Serialize().ToString("base64"),
            "base64url" => Serialize().ToString("base64url"),
            _ => throw new ArgumentException("Unknown mode, supported: hex, fiftBin, fiftHex, base64, base64url")
        };
    }

    public Bits Serialize(
        bool hasIdx = false,
        bool hasCrc32C = true
    )
    {
        return BagOfCells.SerializeBoc(this, hasIdx, hasCrc32C);
    }


    Bits BuildBitsWithDescriptors()
    {
        Bits augmented = Bits.Augment();
        int l = 16 + augmented.Length;
        int d1 = RefsCount + (IsExotic ? 8 : 0); // + MaxLevel * 32;
        int d2 = FullData;
        BitsBuilder bb = new BitsBuilder(l)
            .StoreUInt(d1, 8)
            .StoreUInt(d2, 8)
            .StoreBits(augmented);

        bitsWithDescriptors = bb.Build();
        return bitsWithDescriptors;
    }


    Bits CalcHash()
    {
        Bits bitsWithDescriptors = BitsWithDescriptors;
        int l = bitsWithDescriptors.Length + RefsCount * (16 + 256);
        BitsBuilder bb = new BitsBuilder(l).StoreBits(bitsWithDescriptors, false);
        for (int i = 0; i < RefsCount; i++) bb.StoreUInt(Refs[i].Depth, 16);

        for (int i = 0; i < RefsCount; i++) bb.StoreBits(Refs[i].Hash, false);

        hash = bb.Build().Hash();
        return hash;
    }
}