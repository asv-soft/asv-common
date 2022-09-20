using System;
using System.Collections.Generic;
using System.Linq;

namespace Asv.IO
{
    public abstract class SpanArrayType<T> : ISizedSpanSerializable
    {
        protected abstract void InternalWriteItem(ref Span<byte> buffer, T item);
        protected abstract T InternalReadItem(ref ReadOnlySpan<byte> buffer);
        protected abstract int InternalGetItemsSize(T arg);

        public IList<T> Items { get; } = new List<T>();

        public void Deserialize(ref ReadOnlySpan<byte> buffer)
        {
            var count = BinSerialize.ReadPackedUnsignedInteger(ref buffer);
            for (var i = 0; i < count; i++)
            {
                Items.Add(InternalReadItem(ref buffer));
            }
        }

        public void Serialize(ref Span<byte> buffer)
        {
            BinSerialize.WritePackedUnsignedInteger(ref buffer,(uint)Items.Count);
            foreach (var item in Items)
            {
                InternalWriteItem(ref buffer, item);
            }
        }

        public int GetByteSize()
        {
            return BinSerialize.GetSizeForPackedUnsignedInteger((uint)Items.Count) + Items.Sum(InternalGetItemsSize);
        }
    }
}
