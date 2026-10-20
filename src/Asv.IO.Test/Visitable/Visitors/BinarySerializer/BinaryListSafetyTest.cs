using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using Xunit;

namespace Asv.IO.Test;

public class BinaryListSafetyTest
{
    [Theory]
    [InlineData(0U)]
    [InlineData(3U)]
    [InlineData(1000U)]
    [InlineData(uint.MaxValue)]
    public void Deserialize_CountOutsideSchema_RejectsBeforeConstructingOrReplacingItems(uint count)
    {
        var value = new ListValue(minSize: 1, maxSize: 2);
        var previous = new Cell { Value = 37 };
        value.Items.Add(previous);
        Cell.Created = 0;
        var writer = new ArrayBufferWriter<byte>();
        BinSerialize.WriteUInt(writer, count);
        var memory = writer.WrittenMemory;

        Assert.Throws<InvalidDataException>(() => SimpleBinaryMixin.Deserialize(value, ref memory));

        Assert.Equal(0, Cell.Created);
        Assert.Same(previous, Assert.Single(value.Items));
        Assert.Equal(37, previous.Value);
        Assert.Equal(sizeof(uint), memory.Length);
    }

    [Fact]
    public void Deserialize_AllowedBoundary_PreservesWireFormatAndTrailingData()
    {
        var source = new ListValue(0, 2);
        source.Items.Add(new Cell { Value = 7 });
        source.Items.Add(new Cell { Value = 11 });
        var writer = new ArrayBufferWriter<byte>();
        SimpleBinaryMixin.Serialize(source, writer);
        BinSerialize.WriteByte(writer, 0xA5);
        Assert.Equal(
            new byte[] { 2, 0, 0, 0, 7, 0, 0, 0, 11, 0, 0, 0, 0xA5 },
            writer.WrittenSpan.ToArray()
        );

        var target = new ListValue(0, 2);
        var memory = writer.WrittenMemory;
        SimpleBinaryMixin.Deserialize(target, ref memory);

        Assert.Collection(
            target.Items,
            item => Assert.Equal(7, item.Value),
            item => Assert.Equal(11, item.Value)
        );
        Assert.Equal(new byte[] { 0xA5 }, memory.ToArray());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ExampleMessage1_ExcessListCount_RejectsBeforeResizingThatList(bool structs)
    {
        var source = new ExampleMessage1();
        for (var i = 0; i < 11; i++)
        {
            if (structs)
            {
                source.Value8.Add(new SubObject());
            }
            else
            {
                source.Value9.Add('a');
            }
        }
        var bytes = source.Serialize();
        var target = new ExampleMessage1();
        var previous = new SubObject();
        target.Value8.Add(previous);
        target.Value9.Add('z');

        Assert.Throws<InvalidDataException>(() => target.Deserialize(bytes));
        if (structs)
        {
            Assert.Same(previous, Assert.Single(target.Value8));
        }
        else
        {
            Assert.Equal('z', Assert.Single(target.Value9));
        }
    }

    private sealed class ListValue(int minSize, int maxSize) : IVisitable
    {
        private readonly Field _field = new(
            "items",
            new ListType(Int32Type.Default, minSize, maxSize),
            ImmutableDictionary<string, object?>.Empty
        );

        public List<Cell> Items { get; } = [];

        public void Accept(IVisitor visitor)
        {
            ListType.Accept(
                visitor,
                _field,
                Items,
                (index, current, field, type) =>
                    Int32Type.Accept(current, field, type, ref Items[index].Value)
            );
        }
    }

    private sealed class Cell
    {
        public static int Created;
        public int Value;

        public Cell() => Created++;
    }
}
