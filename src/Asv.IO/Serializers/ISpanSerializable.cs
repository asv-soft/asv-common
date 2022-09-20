using System;

namespace Asv.IO
{
    public interface ISpanSerializable
    {
        void Deserialize(ref ReadOnlySpan<byte> buffer);

        void Serialize(ref Span<byte> buffer);
    }
}
