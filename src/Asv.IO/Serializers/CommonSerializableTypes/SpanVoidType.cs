using System;

namespace Asv.IO
{
    public class SpanVoidType : ISizedSpanSerializable
    {
        public void Deserialize(ref ReadOnlySpan<byte> buffer) { }

        public void Serialize(ref Span<byte> buffer) { }

        public int GetByteSize() => 0;

        public static SpanVoidType Default { get; } = new();

        public override string ToString()
        {
            return "Void";
        }
    }
}
