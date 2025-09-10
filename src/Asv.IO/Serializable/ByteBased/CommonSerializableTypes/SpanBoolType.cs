using System;

namespace Asv.IO
{
    public class SpanBoolType : ISizedSpanSerializable
    {
        public SpanBoolType() { }

        public SpanBoolType(bool value)
        {
            Value = value;
        }

        public bool Value { get; set; }

        public void Deserialize(ref ReadOnlySpan<byte> buffer)
        {
            Value = BinSerialize.ReadByte(ref buffer) != 0;
        }

        public void Serialize(ref Span<byte> buffer)
        {
            BinSerialize.WriteByte(ref buffer, (byte)(Value ? 1 : 0));
        }

        public int GetByteSize() => sizeof(byte);

        public override string ToString()
        {
            return Value.ToString();
        }

        public static explicit operator SpanBoolType(bool value) => new(value);

        public static implicit operator bool(SpanBoolType value) => value.Value;
    }
}
