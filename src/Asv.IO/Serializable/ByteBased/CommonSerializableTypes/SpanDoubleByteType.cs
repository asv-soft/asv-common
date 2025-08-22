using System;

namespace Asv.IO
{
    public class SpanDoubleByteType : ISizedSpanSerializable
    {
        public SpanDoubleByteType()
        {
            
        }

        public SpanDoubleByteType(byte value1,byte value2)
        {
            Value1 = value1;
            Value2 = value2;
        }

        public byte Value1 { get; set; }
        public byte Value2 { get; set; }

        public void Deserialize(ref ReadOnlySpan<byte> buffer)
        {
            Value1 = BinSerialize.ReadByte(ref buffer);
            Value2 = BinSerialize.ReadByte(ref buffer);
        }

        public void Serialize(ref Span<byte> buffer)
        {
            BinSerialize.WriteByte(ref buffer, Value1);
            BinSerialize.WriteByte(ref buffer, Value2);
        }

        public int GetByteSize() => sizeof(byte) * 2;

        public override string ToString()
        {
            return $"({Value1},{Value2})";
        }
    }
}
