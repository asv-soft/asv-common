using System;

namespace Asv.IO
{
    public interface ISpanBitCompactSerializable:ISizedSpanSerializable
    {
        int GetBitSize();
        void Deserialize(ReadOnlySpan<byte> buffer, ref int bitPosition);
        void Serialize(Span<byte> buffer, ref int bitPosition);
    }

    public abstract class SpanBitCompactSerializableBase : ISpanBitCompactSerializable
    {
        public void Deserialize(ref ReadOnlySpan<byte> buffer)
        {
            int bitIndex = 0;
            Deserialize(buffer, ref bitIndex);
            buffer = bitIndex % 8.0 == 0 ? buffer.Slice(bitIndex / 8) : buffer.Slice(bitIndex / 8 + 1);
        }

        public void Serialize(ref Span<byte> buffer)
        {
            int bitIndex = 0;
            Serialize(buffer, ref bitIndex);
            buffer = bitIndex % 8.0 == 0 ? buffer.Slice(bitIndex / 8) : buffer.Slice(bitIndex / 8 + 1);
        }

        public int GetByteSize()
        {
            var bitSize = GetBitSize();
            var size = bitSize / 8;
            return bitSize % 8.0 == 0 ? size : size + 1;
        }

        public abstract int GetBitSize();
        public abstract void Deserialize(ReadOnlySpan<byte> buffer, ref int bitPosition);
        public abstract void Serialize(Span<byte> buffer, ref int bitPosition);

    }
}
