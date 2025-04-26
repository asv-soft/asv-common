
using System;
using System.Buffers;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using JetBrains.Annotations;
using Xunit;

namespace Asv.IO.Test;

[TestSubject(typeof(SpanSerializableMixin))]
public class SpanSerializableMixinTest
{
    private class TestSerializable : ISizedSpanSerializable
    {
        public byte[] Data { get; set; } = Array.Empty<byte>();

        public void Serialize(ref Span<byte> buffer)
        {
            if (buffer.Length < Data.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(buffer), "Buffer is too small to serialize the data.");
            }
            var spanLength = Math.Min(buffer.Length, Data.Length);
            Data.AsSpan(0, spanLength).CopyTo(buffer);
            buffer = buffer[spanLength..];
        }

        public void Deserialize(ref ReadOnlySpan<byte> buffer)
        {
            Data = buffer.ToArray();
            buffer = buffer[buffer.Length..];
        }

        public int GetByteSize() => Data.Length;
    }

    #region Serialize To Byte Array

    [Fact]
    public void Serialize_ToByteArray_ShouldSerializeCorrectly()
    {
        var testObject = new TestSerializable { Data = new byte[] { 1, 2, 3, 4, 5 } };
        var buffer = new byte[10];

        var result = testObject.Serialize(buffer);

        result.Should().Be(5);
        buffer[..result].Should().BeEquivalentTo(testObject.Data);
    }

    [Fact]
    public void Serialize_ToByteArray_WithStartingIndex_ShouldSerializeCorrectly()
    {
        var testObject = new TestSerializable { Data = new byte[] { 1, 2, 3, 4, 5 } };
        var buffer = new byte[10];

        var result = testObject.Serialize(buffer, 5);

        result.Should().Be(5);
        buffer[5..(5 + result)].Should().BeEquivalentTo(testObject.Data);
    }

    [Fact]
    public void Serialize_ToByteArray_WithInsufficientLength_ShouldThrow()
    {
        var testObject = new TestSerializable { Data = new byte[] { 1, 2, 3, 4, 5 } };
        var buffer = new byte[3];

        Action action = () => testObject.Serialize(buffer);

        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    #endregion

    #region Deserialize From Byte Array

    [Fact]
    public void Deserialize_FromByteArray_ShouldDeserializeCorrectly()
    {
        var buffer = new byte[] { 1, 2, 3, 4, 5 };
        var testObject = new TestSerializable();

        var result = testObject.Deserialize(buffer);

        result.Should().Be(5);
        testObject.Data.Should().BeEquivalentTo(buffer);
    }

    [Fact]
    public void Deserialize_FromByteArray_WithStartingIndex_ShouldDeserializeCorrectly()
    {
        var buffer = new byte[10];
        Array.Copy(new byte[] { 1, 2, 3, 4, 5 }, 0, buffer, 5, 5);
        var testObject = new TestSerializable();

        var result = testObject.Deserialize(buffer, 5);

        result.Should().Be(5);
        testObject.Data.Should().BeEquivalentTo(buffer[5..(5 + result)]);
    }

    #endregion

    #region Serialize To Memory

    [Fact]
    public async Task Serialize_ToMemory_ShouldSerializeCorrectly()
    {
        var testObject = new TestSerializable { Data = new byte[] { 1, 2, 3, 4, 5 } };
        var buffer = new byte[10];

        var result = await testObject.Serialize(new Memory<byte>(buffer));

        result.Should().Be(5);
        buffer[..result].Should().BeEquivalentTo(testObject.Data);
    }

    #endregion

    #region Deserialize From ReadOnlyMemory

    [Fact]
    public async Task Deserialize_FromReadOnlyMemory_ShouldDeserializeCorrectly()
    {
        var buffer = new byte[] { 1, 2, 3, 4, 5 };
        var testObject = new TestSerializable();

        var result = await testObject.Deserialize(new ReadOnlyMemory<byte>(buffer));

        result.Should().Be(5);
        testObject.Data.Should().BeEquivalentTo(buffer);
    }

    #endregion

    #region Serialize To BufferWriter

    [Fact]
    public void Serialize_ToBufferWriter_ShouldSerializeCorrectly()
    {
        var testObject = new TestSerializable { Data = new byte[] { 1, 2, 3, 4, 5 } };
        var bufferWriter = new ArrayBufferWriter<byte>();

        testObject.Serialize(bufferWriter);

        bufferWriter.WrittenSpan.ToArray().Should().BeEquivalentTo(testObject.Data);
    }

    #endregion

    #region Deserialize From Stream

    [Fact]
    public void Deserialize_FromStream_ShouldDeserializeCorrectly()
    {
        var stream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });
        var testObject = new TestSerializable();

        testObject.Deserialize(stream, 5);

        testObject.Data.Should().BeEquivalentTo(new byte[] { 1, 2, 3, 4, 5 });
    }

    [Fact]
    public void Deserialize_FromStream_WithInsufficientData_ShouldThrow()
    {
        var stream = new MemoryStream(new byte[] { 1, 2 });
        var testObject = new TestSerializable();

        Action action = () => testObject.Deserialize(stream, 5);

        action.Should().Throw<Exception>().WithMessage("*Want read 5 bytes. Got 2 bytes*");
    }

    #endregion

    #region BinaryClone

    [Fact]
    public void BinaryClone_ShouldCloneCorrectly()
    {
        var original = new TestSerializable { Data = new byte[] { 1, 2, 3, 4, 5 } };

        var clone = original.BinaryClone();

        clone.Should().NotBeSameAs(original);
        clone.Data.Should().BeEquivalentTo(original.Data);
    }

    #endregion

    #region CopyTo

    [Fact]
    public void CopyTo_ShouldCopyDataCorrectly()
    {
        var source = new TestSerializable { Data = new byte[] { 1, 2, 3, 4, 5 } };
        var destination = new TestSerializable();

        source.CopyTo(destination);

        destination.Data.Should().BeEquivalentTo(source.Data);
    }

    #endregion
}