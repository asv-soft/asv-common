using System.Buffers;
using Asv.IO;
using DeepEqual.Syntax;
using JetBrains.Annotations;
using Xunit;

namespace Asv.IO.Test.Visitable.Visitors.BinarySerializer;

[TestSubject(typeof(SimpleBinaryMixin))]
public class SimpleBinaryMixinTest
{
    [Fact]
    public void CalculateSize_ByVisitorWithSimpleStruct_Success()
    {
        var example = new SubObject().Randomize();
        Assert.Equal(example.GetByteSize(), SimpleBinaryMixin.GetSize(example));
    }

    [Fact]
    public void CalculateSize_ByVisitorWithMessage_Success()
    {
        var example = new ExampleMessage1().Randomize();
        Assert.Equal(
            example.GetByteSize() - 5, /*HEADER + CRC*/
            SimpleBinaryMixin.GetSize(example)
        );
    }

    [Fact]
    public void CalculateSize_SerializeWithSimpleStruct_Success()
    {
        var origin = new SubObject().Randomize();
        var size = SimpleBinaryMixin.GetSize(origin);
        var b = new ArrayBufferWriter<byte>(size);

        SimpleBinaryMixin.Serialize(origin, b);

        var writeMem = b.WrittenMemory;

        var deserialized = new SubObject();
        SimpleBinaryMixin.Deserialize(deserialized, ref writeMem);

        Assert.Equal(origin, deserialized);
    }

    [Fact]
    public void CalculateSize_SerializeWithMessage_Success()
    {
        var origin = new ExampleMessage1().Randomize();
        var size = SimpleBinaryMixin.GetSize(origin);
        var b = new ArrayBufferWriter<byte>(size);

        SimpleBinaryMixin.Serialize(origin, b);

        var writeMem = b.WrittenMemory;

        var deserialized = new ExampleMessage1();
        SimpleBinaryMixin.Deserialize(deserialized, ref writeMem);

        origin.ShouldDeepEqual(deserialized);
    }
}
