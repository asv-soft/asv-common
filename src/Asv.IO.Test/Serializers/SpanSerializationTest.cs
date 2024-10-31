using System;
using Xunit;
using Xunit.Abstractions;

namespace Asv.IO.Test
{
    public class SpanSerializationTest
    {
        private readonly ITestOutputHelper _output;

        public SpanSerializationTest(ITestOutputHelper output)
        {
            _output = output;
        }

        public class TestType : SpanKeyWithNameType<int>
        {
            protected override void InternalValidateName(string name) { }

            protected override int InternalReadKey(ref ReadOnlySpan<byte> buffer)
            {
                return BinSerialize.ReadPackedInteger(ref buffer);
            }

            protected override void InternalWriteKey(ref Span<byte> buffer, int id)
            {
                BinSerialize.WritePackedInteger(ref buffer, id);
            }

            protected override int InternalGetSizeKey(int id)
            {
                return BinSerialize.GetSizeForPackedInteger(id);
            }
        }

        [Fact]
        public void StandartTypes()
        {
            SpanTestHelper.SerializeDeserializeTestBegin(_output.WriteLine);
            var data = new byte[256];
            new Random().NextBytes(data);
            SpanTestHelper.TestType(new SpanVoidType(), _output.WriteLine);
            SpanTestHelper.TestType(new SpanBoolType(true), _output.WriteLine);
            SpanTestHelper.TestType(new SpanBoolType(false), _output.WriteLine);
            SpanTestHelper.TestType(new SpanByteArrayType(data), _output.WriteLine);
            SpanTestHelper.TestType(new SpanByteType(byte.MaxValue), _output.WriteLine);
            SpanTestHelper.TestType(new SpanByteType(byte.MinValue), _output.WriteLine);
            SpanTestHelper.TestType(
                new SpanDoubleByteType(byte.MinValue, byte.MaxValue),
                _output.WriteLine
            );
            SpanTestHelper.TestType(
                new SpanPacketUnsignedIntegerType(uint.MaxValue),
                _output.WriteLine
            );
            SpanTestHelper.TestType(new SpanPacketIntegerType(int.MaxValue), _output.WriteLine);
            SpanTestHelper.TestType(new SpanStringType("asdasd ASDSAD 984984"), _output.WriteLine);
            SpanTestHelper.TestType(new SpanByteArrayType(data), _output.WriteLine);
            SpanTestHelper.TestType(
                new TestType { Id = new Random().Next(), Name = "asdasd" },
                _output.WriteLine
            );
        }
    }
}
