using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TonSdk.Core.boc.bits;
using TonSdk.Core.boc.Cells;

namespace TonSdk.Core.boc;

public struct HashmapSerializers<TK, TV>
{
    public Func<TK, Bits> Key;
    public Func<TV, Cell> Value;
}

public struct HashmapDeserializers<TK, TV>
{
    public Func<Bits, TK> Key;
    public Func<Cell, TV> Value;
}

public class HashmapOptions<TK, TV>
{
    public HashmapDeserializers<TK, TV>? Deserializers;
    public uint KeySize;

    public HashmapSerializers<TK, TV>? Serializers;
}

public struct HmapNodeSer
{
    public BitsSlice Key;
    public Cell Value;
}

public struct HmapNode
{
    public Bits Key;
    public Cell Value;
}

public abstract class HashmapBase<T, TK, TV> where T : HashmapBase<T, TK, TV>
{
    protected Func<Bits, TK>? DeserializeKey;
    protected Func<Cell, TV>? DeserializeValue;
    protected uint KeySize;

    protected SortedDictionary<Bits, Cell> Map;
    protected Func<TK, Bits>? SerializeKey;
    protected Func<TV, Cell>? SerializeValue;

    public HashmapBase(HashmapOptions<TK, TV> opt)
    {
        if (opt.KeySize == 0) throw new Exception("Key size can not be 0");

        Map = new SortedDictionary<Bits, Cell>();
        KeySize = opt.KeySize;
        SerializeKey = opt.Serializers?.Key;
        SerializeValue = opt.Serializers?.Value;
        DeserializeKey = opt.Deserializers?.Key;
        DeserializeValue = opt.Deserializers?.Value;
    }

    public int Count => Map.Count;

    protected void CheckSerializers()
    {
        if (SerializeKey == null || SerializeValue == null) throw new Exception("Serializers are not set");
    }

    protected void CheckDeserializers()
    {
        if (DeserializeKey == null || DeserializeValue == null) throw new Exception("Deserializers are not set");
    }

    public T Set(TK key, TV value)
    {
        CheckSerializers();

        Bits? k = SerializeKey!(key);
        Cell? v = SerializeValue!(value);

        return SetRaw(k, v);
    }

    protected T SetRaw(Bits key, Cell value)
    {
        if (key.Length != KeySize) throw new Exception("Wrong key size");
        Map[key] = value;
        return (T)this;
    }

    public TV Get(TK key)
    {
        CheckSerializers();
        CheckDeserializers();

        Bits? k = SerializeKey(key);

        if (k.Length != KeySize) throw new Exception("Wrong key size");

        Cell cell;
        if (Map.TryGetValue(k, out cell)) return DeserializeValue(cell);

        // If V is a value type, you need to provide some default value
        // If V is a class, you can return null. But be aware of NullReferenceException
        return default;
    }


    /// <summary>
    ///     Serialize Hashmap object to TVM Cell
    /// </summary>
    /// <returns>TVM Cell</returns>
    public Cell? Serialize()
    {
        List<HmapNodeSer> nodes = new(Map.Count);

        nodes.AddRange(Map.Select(kvp => new HmapNodeSer { Key = kvp.Key.Parse(), Value = kvp.Value }));

        return nodes.Count == 0 ? null : SerializeEdge(nodes);
    }

    protected Cell SerializeEdge(List<HmapNodeSer> nodes)
    {
        if (nodes.Count == 0)
            return new CellBuilder()
                .StoreBits(SerializeLabelShort(new Bits(0)))
                .Build();

        CellBuilder edge = new();
        Bits label = SerializeLabel(nodes);

        edge.StoreBits(label);

        // hmn_leaf#_ {X:Type} value:X = HashmapNode 0 X;
        if (nodes.Count == 1)
        {
            Cell leaf = SerializeLeaf(nodes[0]);
            edge.StoreCellSlice(leaf.Parse());
        }

        // hmn_fork#_ {n:#} {X:Type} left:^(Hashmap n X) right:^(Hashmap n X) = HashmapNode (n + 1) X;
        if (nodes.Count > 1)
        {
            (List<HmapNodeSer> leftNodes, List<HmapNodeSer> rightNodes) = SerializeFork(nodes);
            Cell leftEdge = SerializeEdge(leftNodes);

            edge.StoreRef(leftEdge);

            if (rightNodes.Count > 0)
            {
                Cell rightEdge = SerializeEdge(rightNodes);

                edge.StoreRef(rightEdge);
            }
        }

        return edge.Build();
    }

    protected (List<HmapNodeSer>, List<HmapNodeSer>) SerializeFork(List<HmapNodeSer> nodes)
    {
        List<HmapNodeSer> leftNodes = new(nodes.Count);
        List<HmapNodeSer> rightNodes = new(nodes.Count);

        foreach (HmapNodeSer node in nodes)
            if (node.Key.LoadBit()) rightNodes.Add(node);
            else leftNodes.Add(node);

        return (leftNodes, rightNodes);
    }

    protected Cell SerializeLeaf(HmapNodeSer node)
    {
        return node.Value;
    }

    protected Bits SerializeLabel(List<HmapNodeSer> nodes)
    {
        static Bits getRepeated(Bits b)
        {
            if (b.Length == 0) return new Bits(0);

            BitsSlice bs = b.Parse();
            bool f = bs.LoadBit();

            BitsBuilder bb = new BitsBuilder().StoreBit(f);
            for (int i = 1; i < b.Length; i++)
            {
                if (bs.LoadBit() != f) return bb.Build();
                bb.StoreBit(f);
            }

            return bb.Build();
        }
        // Each label can always be serialized in at least two different fashions, using
        // hml_short or hml_long constructors. Usually the shortest serialization (and
        // in the case of a tie—the lexicographically smallest among the shortest) is
        // preferred and is generated by TVM hashmap primitives, while the other
        // variants are still considered valid.

        BitsSlice first = nodes[0].Key;
        BitsSlice last = nodes[nodes.Count - 1].Key;


        // m = length at most possible bits of n (key)
        int m = first.RemainderBits;
        int sameBitsIndex = -1;

        for (int i = 0; i < m; i++)
            if (first.ReadBit(i) != last.ReadBit(i))
            {
                sameBitsIndex = i;
                break;
            }

        int sameBitsLength = sameBitsIndex == -1 ? first.RemainderBits : sameBitsIndex;

        // hml_short$0 {m:#} {n:#} len:(Unary ~n) s:(n * Bit) = HmLabel ~n m;
        if (m == 0 || first.ReadBit() != last.ReadBit()) return SerializeLabelShort(new Bits(0));

        Bits label = first.ReadBits(sameBitsLength);
        Bits repeated = getRepeated(label);
        Bits labelShort = SerializeLabelShort(label);
        Bits labelLong = SerializeLabelLong(label, m);
        Bits? labelSame = nodes.Count > 1 && repeated.Length > 1
            ? SerializeLabelSame(repeated, m)
            : null;

        List<(int, Bits?)> labels = new List<(int, Bits?)>
        {
            (label.Length, labelShort),
            (label.Length, labelLong),
            (repeated.Length, labelSame)
        }.Where(el => el.Item2 != null).ToList();


        // Sort labels by their length
        labels.Sort((a, b) => a.Item2!.Length - b.Item2!.Length);

        // Get most compact label
        (int, Bits?) chosen = labels[0];

        // Remove label bits from nodes keys
        foreach (HmapNodeSer node in nodes) node.Key.SkipBits(chosen.Item1);

        return chosen.Item2!;
    }

    // hml_short$0 {m:#} {n:#} len:(Unary ~n) {n <= m} s:(n * Bit) = HmLabel ~n m;
    protected Bits SerializeLabelShort(Bits bits)
    {
        if (bits.Length == 0)
            return new BitsBuilder()
                .StoreBit(false)
                .StoreBit(false)
                .Build();
        return new BitsBuilder()
            .StoreBit(false)
            .StoreInt(-1, bits.Length)
            .StoreBit(false)
            .StoreBits(bits)
            .Build();
    }

    // hml_long$10 {m:#} n:(#<= m) s:(n * Bit) = HmLabel ~n m;
    protected Bits SerializeLabelLong(Bits bits, int m)
    {
        return new BitsBuilder()
            .StoreBit(true).StoreBit(false)
            .StoreUInt(bits.Length, (int)Math.Ceiling(Math.Log(m + 1, 2)))
            .StoreBits(bits)
            .Build();
    }

    // hml_same$11 {m:#} v:Bit n:(#<= m) = HmLabel ~n m;
    protected Bits SerializeLabelSame(Bits bits, int m)
    {
        return new BitsBuilder()
            .StoreBit(true).StoreBit(true)
            .StoreBit(bits.Data[0])
            .StoreUInt(bits.Length, (int)Math.Ceiling(Math.Log(m + 1, 2)))
            .Build();
    }

    protected static List<HmapNode> DeserializeEdge(CellSlice edge, uint keySize, BitsBuilder? key = null)
    {
        key ??= new BitsBuilder((int)keySize);

        Bits label = DeserializeLabel(edge, key.RemainderBits);
        Bits keyBits = key.StoreBits(label).Build();

        if (keyBits.Length == keySize)
        {
            Cell value = new CellBuilder().StoreCellSlice(edge).Build();
            return new List<HmapNode> { new() { Key = keyBits, Value = value } };
        }


        if (edge.RemainderRefs < 1 || edge.RemainderRefs > 2)
            throw new Exception("Hashmap: invalid hashmap structure");

        List<HmapNode> nodes = new();
        int bit = 0;
        while (edge.RemainderRefs > 0)
        {
            CellSlice forkEdge = edge.LoadRef().Parse();
            BitsBuilder forkKey = new BitsBuilder((int)keySize).StoreBits(keyBits).StoreBit(bit == 1);
            bit++;
            nodes.AddRange(DeserializeEdge(forkEdge, keySize, forkKey));
        }

        return nodes;
    }

    protected static Bits DeserializeLabel(CellSlice edge, long m)
    {
        // m = length at most possible bits of n (key)

        // hml_short$0
        if (!edge.LoadBit()) return DeserializeLabelShort(edge);

        // hml_long$10
        if (!edge.LoadBit()) return DeserializeLabelLong(edge, m);

        // hml_same$11
        return DeserializeLabelSame(edge, m);
    }

    protected static Bits DeserializeLabelShort(CellSlice edge)
    {
        int length = 0;
        while (edge.LoadBit()) length++;

        return edge.LoadBits(length);
    }

    protected static Bits DeserializeLabelLong(CellSlice edge, long m)
    {
        int length = (int)edge.LoadUInt((int)Math.Ceiling(Math.Log(m + 1, 2)));

        return edge.LoadBits(length);
    }

    protected static Bits DeserializeLabelSame(CellSlice edge, long m)
    {
        bool repeated = edge.LoadBit();
        int length = (int)edge.LoadUInt((int)Math.Ceiling(Math.Log(m + 1, 2)));

        return new Bits(new BitArray(length, repeated));
    }

    /// <summary>
    ///     Alias for Hashmap.Serialize();
    ///     Serialize Hashmap to TVM Cell
    /// </summary>
    /// <returns>
    ///     Cell or null (Maybe Cell)
    /// </returns>
    public virtual Cell? Build()
    {
        return Serialize();
    }
}

public class Hashmap<TK, TV>(HashmapOptions<TK, TV> opt) : HashmapBase<Hashmap<TK, TV>, TK, TV>(opt)
{
    /// <summary>
    ///     Deserializes hashmap from TVM Cell to C# object
    /// </summary>
    /// <param name="dictCell">Dictionary TVM Cell</param>
    /// <param name="opt">
    ///     Hashmap options: KeySize, Serializers, Deserializers, etc.
    /// </param>
    /// <typeparam name="TK">Type of hashmap Key (after deserialize)</typeparam>
    /// <typeparam name="TV">type of hashmap Value (after deserialize)</typeparam>
    /// <returns>Hashmap object</returns>
    public static Hashmap<TK, TV> Deserialize<TK, TV>(Cell? dictCell, HashmapOptions<TK, TV> opt)
    {
        if (dictCell == null) return new Hashmap<TK, TV>(opt);
        if (dictCell.BitsCount < 2)
            throw new Exception("Hashmap: can't be empty. It must contain at least 1 key-value pair.");

        Hashmap<TK, TV> hashmap = new(opt);
        CellSlice dictSlice = dictCell.Parse();

        List<HmapNode> nodes = DeserializeEdge(dictSlice, opt.KeySize);

        foreach (HmapNode node in nodes) hashmap.SetRaw(node.Key, node.Value);

        return hashmap;
    }

    /// <summary>
    ///     Alias for Hashmap.Deserialize();
    ///     Deserializes hashmap from TVM Cell to C# object
    /// </summary>
    /// <param name="dictCell">Dictionary TVM Cell</param>
    /// <param name="opt">
    ///     Hashmap options: KeySize, Serializers, Deserializers, etc.
    /// </param>
    /// <typeparam name="TK">Type of hashmap Key (after deserialize)</typeparam>
    /// <typeparam name="TV">type of hashmap Value (after deserialize)</typeparam>
    /// <returns>Hashmap object</returns>
    public Hashmap<TK, TV> Parse(Cell dictCell, HashmapOptions<TK, TV> opt)
    {
        return Deserialize(dictCell, opt);
    }
}

public class HashmapE<TK, TV>(HashmapOptions<TK, TV> opt) : HashmapBase<HashmapE<TK, TV>, TK, TV>(opt)
{
    /// <summary>
    ///     Serializes hashmap from C# object to TVM Cell
    /// </summary>
    /// <returns>Cell</returns>
    public new Cell Serialize()
    {
        Cell? dict = base.Serialize();

        if (dict == null)
            return new CellBuilder()
                .StoreBit(false)
                .Build();

        return new CellBuilder()
            .StoreBit(true)
            .StoreRef(dict)
            .Build();
    }

    /// <summary>
    ///     Alias for HashmapE.Serialize();
    ///     Serialize HashmapE to TVM Cell
    /// </summary>
    /// <returns>
    ///     Cell
    /// </returns>
    public new Cell Build()
    {
        return Serialize();
    }

    /// <summary>
    ///     Deserializes hashmap from TVM CellSlice to C# object
    /// </summary>
    /// <param name="dictSlice">TVM CellSlice includes dictionary</param>
    /// <param name="opt">
    ///     Hashmap options: KeySize, Serializers, Deserializers, etc.
    /// </param>
    /// <typeparam name="TK">Type of hashmap Key (after deserialize)</typeparam>
    /// <typeparam name="TV">type of hashmap Value (after deserialize)</typeparam>
    /// <returns>HashmapE object</returns>
    public static HashmapE<TK, TV> Deserialize(CellSlice dictSlice, HashmapOptions<TK, TV> opt, bool inplace = true)
    {
        bool maybeBit = dictSlice.ReadBit();

        HashmapE<TK, TV> hashmap = new(opt);

        if (!maybeBit)
        {
            if (inplace) dictSlice.SkipBit();
            return hashmap;
        }

        Cell dictCell = dictSlice.ReadRef();

        List<HmapNode> nodes = DeserializeEdge(dictCell.Parse(), opt.KeySize);

        foreach (HmapNode node in nodes) hashmap.SetRaw(node.Key, node.Value);

        if (inplace) dictSlice.SkipBit().SkipRef();
        return hashmap;
    }

    /// <summary>
    ///     Alias for HashmapE.Deserialize();
    ///     Deserializes hashmap from TVM CellSlice to C# object
    /// </summary>
    /// <param name="dictSlice">TVM CellSlice includes dictionary</param>
    /// <param name="opt">
    ///     Hashmap options: KeySize, Serializers, Deserializers, etc.
    /// </param>
    /// <typeparam name="TK">Type of hashmap Key (after deserialize)</typeparam>
    /// <typeparam name="TV">type of hashmap Value (after deserialize)</typeparam>
    /// <returns>HashmapE object</returns>
    public static HashmapE<TK, TV> Parse(CellSlice dictSlice, HashmapOptions<TK, TV> opt)
    {
        return Deserialize(dictSlice, opt);
    }
}