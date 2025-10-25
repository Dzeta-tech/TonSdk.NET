using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace TonSdk.Core.Boc
{
    public struct HashmapSerializers<K, V>
    {
        public Func<K, Bits> Key;
        public Func<V, Cell> Value;
    }

    public struct HashmapDeserializers<K, V>
    {
        public Func<Bits, K> Key;
        public Func<Cell, V> Value;
    }

    public class HashmapOptions<K, V>
    {
        public HashmapDeserializers<K, V>? Deserializers;
        public uint KeySize;

        public HashmapSerializers<K, V>? Serializers;
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

    public abstract class HashmapBase<T, K, V> where T : HashmapBase<T, K, V>
    {
        protected Func<Bits, K>? deserializeKey;
        protected Func<Cell, V>? deserializeValue;
        protected uint keySize;

        protected SortedDictionary<Bits, Cell> map;
        protected Func<K, Bits>? serializeKey;
        protected Func<V, Cell>? serializeValue;

        public HashmapBase(HashmapOptions<K, V> opt)
        {
            if (opt.KeySize == 0) throw new Exception("Key size can not be 0");

            map = new SortedDictionary<Bits, Cell>();
            keySize = opt.KeySize;
            serializeKey = opt.Serializers?.Key;
            serializeValue = opt.Serializers?.Value;
            deserializeKey = opt.Deserializers?.Key;
            deserializeValue = opt.Deserializers?.Value;
        }

        public int Count => map.Count;

        protected void CheckSerializers()
        {
            if (serializeKey == null || serializeValue == null) throw new Exception("Serializers are not set");
        }

        protected void CheckDeserializers()
        {
            if (deserializeKey == null || deserializeValue == null) throw new Exception("Deserializers are not set");
        }

        public T Set(K key, V value)
        {
            CheckSerializers();

            Bits? k = serializeKey!(key);
            Cell? v = serializeValue!(value);

            return SetRaw(k, v);
        }

        protected T SetRaw(Bits key, Cell value)
        {
            if (key.Length != keySize) throw new Exception("Wrong key size");
            map[key] = value;
            return (T)this;
        }

        public V Get(K key)
        {
            CheckSerializers();
            CheckDeserializers();

            Bits? k = serializeKey(key);

            if (k.Length != keySize) throw new Exception("Wrong key size");

            Cell cell;
            if (map.TryGetValue(k, out cell)) return deserializeValue(cell);

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
            List<HmapNodeSer> nodes = new List<HmapNodeSer>(map.Count);

            nodes.AddRange(map.Select(kvp => new HmapNodeSer { Key = kvp.Key.Parse(), Value = kvp.Value }));

            return nodes.Count == 0 ? null : serializeEdge(nodes);
        }

        protected Cell serializeEdge(List<HmapNodeSer> nodes)
        {
            if (nodes.Count == 0)
                return new CellBuilder()
                    .StoreBits(serializeLabelShort(new Bits(0)))
                    .Build();

            CellBuilder edge = new CellBuilder();
            Bits label = serializeLabel(nodes);

            edge.StoreBits(label);

            // hmn_leaf#_ {X:Type} value:X = HashmapNode 0 X;
            if (nodes.Count == 1)
            {
                Cell leaf = serializeLeaf(nodes[0]);
                edge.StoreCellSlice(leaf.Parse());
            }

            // hmn_fork#_ {n:#} {X:Type} left:^(Hashmap n X) right:^(Hashmap n X) = HashmapNode (n + 1) X;
            if (nodes.Count > 1)
            {
                (List<HmapNodeSer> leftNodes, List<HmapNodeSer> rightNodes) = serializeFork(nodes);
                Cell leftEdge = serializeEdge(leftNodes);

                edge.StoreRef(leftEdge);

                if (rightNodes.Count > 0)
                {
                    Cell rightEdge = serializeEdge(rightNodes);

                    edge.StoreRef(rightEdge);
                }
            }

            return edge.Build();
        }

        protected (List<HmapNodeSer>, List<HmapNodeSer>) serializeFork(List<HmapNodeSer> nodes)
        {
            List<HmapNodeSer> leftNodes = new List<HmapNodeSer>(nodes.Count);
            List<HmapNodeSer> rightNodes = new List<HmapNodeSer>(nodes.Count);

            foreach (HmapNodeSer node in nodes)
                if (node.Key.LoadBit()) rightNodes.Add(node);
                else leftNodes.Add(node);

            return (leftNodes, rightNodes);
        }

        protected Cell serializeLeaf(HmapNodeSer node)
        {
            return node.Value;
        }

        protected Bits serializeLabel(List<HmapNodeSer> nodes)
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
            if (m == 0 || first.ReadBit() != last.ReadBit()) return serializeLabelShort(new Bits(0));

            Bits label = first.ReadBits(sameBitsLength);
            Bits repeated = getRepeated(label);
            Bits labelShort = serializeLabelShort(label);
            Bits labelLong = serializeLabelLong(label, m);
            Bits? labelSame = nodes.Count > 1 && repeated.Length > 1
                ? serializeLabelSame(repeated, m)
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
        protected Bits serializeLabelShort(Bits bits)
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
        protected Bits serializeLabelLong(Bits bits, int m)
        {
            return new BitsBuilder()
                .StoreBit(true).StoreBit(false)
                .StoreUInt(bits.Length, (int)Math.Ceiling(Math.Log(m + 1, 2)))
                .StoreBits(bits)
                .Build();
        }

        // hml_same$11 {m:#} v:Bit n:(#<= m) = HmLabel ~n m;
        protected Bits serializeLabelSame(Bits bits, int m)
        {
            return new BitsBuilder()
                .StoreBit(true).StoreBit(true)
                .StoreBit(bits.Data[0])
                .StoreUInt(bits.Length, (int)Math.Ceiling(Math.Log(m + 1, 2)))
                .Build();
        }

        protected static List<HmapNode> deserializeEdge(CellSlice edge, uint keySize, BitsBuilder? key = null)
        {
            key ??= new BitsBuilder((int)keySize);

            Bits label = deserializeLabel(edge, key.RemainderBits);
            Bits keyBits = key.StoreBits(label).Build();

            if (keyBits.Length == keySize)
            {
                Cell value = new CellBuilder().StoreCellSlice(edge).Build();
                return new List<HmapNode> { new HmapNode { Key = keyBits, Value = value } };
            }


            if (edge.RemainderRefs < 1 || edge.RemainderRefs > 2)
                throw new Exception("Hashmap: invalid hashmap structure");

            List<HmapNode> nodes = new List<HmapNode>();
            int bit = 0;
            while (edge.RemainderRefs > 0)
            {
                CellSlice forkEdge = edge.LoadRef().Parse();
                BitsBuilder forkKey = new BitsBuilder((int)keySize).StoreBits(keyBits).StoreBit(bit == 1);
                bit++;
                nodes.AddRange(deserializeEdge(forkEdge, keySize, forkKey));
            }

            return nodes;
        }

        protected static Bits deserializeLabel(CellSlice edge, long m)
        {
            // m = length at most possible bits of n (key)

            // hml_short$0
            if (!edge.LoadBit()) return deserializeLabelShort(edge);

            // hml_long$10
            if (!edge.LoadBit()) return deserializeLabelLong(edge, m);

            // hml_same$11
            return deserializeLabelSame(edge, m);
        }

        protected static Bits deserializeLabelShort(CellSlice edge)
        {
            int length = 0;
            while (edge.LoadBit()) length++;

            return edge.LoadBits(length);
        }

        protected static Bits deserializeLabelLong(CellSlice edge, long m)
        {
            int length = (int)edge.LoadUInt((int)Math.Ceiling(Math.Log(m + 1, 2)));

            return edge.LoadBits(length);
        }

        protected static Bits deserializeLabelSame(CellSlice edge, long m)
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

    public class Hashmap<K, V> : HashmapBase<Hashmap<K, V>, K, V>
    {
        public Hashmap(HashmapOptions<K, V> opt) : base(opt)
        {
        }

        /// <summary>
        ///     Deserializes hashmap from TVM Cell to C# object
        /// </summary>
        /// <param name="dictCell">Dictionary TVM Cell</param>
        /// <param name="opt">
        ///     Hashmap options: KeySize, Serializers, Deserializers, etc.
        /// </param>
        /// <typeparam name="K">Type of hashmap Key (after deserialize)</typeparam>
        /// <typeparam name="V">type of hashmap Value (after deserialize)</typeparam>
        /// <returns>Hashmap object</returns>
        public static Hashmap<K, V> Deserialize<K, V>(Cell? dictCell, HashmapOptions<K, V> opt)
        {
            if (dictCell == null) return new Hashmap<K, V>(opt);
            if (dictCell.BitsCount < 2)
                throw new Exception("Hashmap: can't be empty. It must contain at least 1 key-value pair.");

            Hashmap<K, V> hashmap = new Hashmap<K, V>(opt);
            CellSlice dictSlice = dictCell.Parse();

            List<HmapNode> nodes = deserializeEdge(dictSlice, opt.KeySize);

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
        /// <typeparam name="K">Type of hashmap Key (after deserialize)</typeparam>
        /// <typeparam name="V">type of hashmap Value (after deserialize)</typeparam>
        /// <returns>Hashmap object</returns>
        public Hashmap<K, V> Parse(Cell dictCell, HashmapOptions<K, V> opt)
        {
            return Deserialize(dictCell, opt);
        }
    }


    public class HashmapE<K, V> : HashmapBase<HashmapE<K, V>, K, V>
    {
        public HashmapE(HashmapOptions<K, V> opt) : base(opt)
        {
        }

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
        /// <typeparam name="K">Type of hashmap Key (after deserialize)</typeparam>
        /// <typeparam name="V">type of hashmap Value (after deserialize)</typeparam>
        /// <returns>HashmapE object</returns>
        public static HashmapE<K, V> Deserialize(CellSlice dictSlice, HashmapOptions<K, V> opt, bool inplace = true)
        {
            bool maybeBit = dictSlice.ReadBit();

            HashmapE<K, V> hashmap = new HashmapE<K, V>(opt);

            if (!maybeBit)
            {
                if (inplace) dictSlice.SkipBit();
                return hashmap;
            }

            Cell dictCell = dictSlice.ReadRef();

            List<HmapNode> nodes = deserializeEdge(dictCell.Parse(), opt.KeySize);

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
        /// <typeparam name="K">Type of hashmap Key (after deserialize)</typeparam>
        /// <typeparam name="V">type of hashmap Value (after deserialize)</typeparam>
        /// <returns>HashmapE object</returns>
        public static HashmapE<K, V> Parse(CellSlice dictSlice, HashmapOptions<K, V> opt)
        {
            return Deserialize(dictSlice, opt);
        }
    }
}