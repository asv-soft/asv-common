using System;
using System.Buffers;
using System.Threading.Tasks;
using R3;
using Xunit;

namespace Asv.IO.Test;

public class ExampleParserTest
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Dispose_ReturnsBufferExactlyOnceAndRejectsFurtherAccess(bool asynchronous)
    {
        var pool = new ExactPool();
        var parser = CreateParser(pool);
        Assert.Equal(260, pool.Rented!.Length);
        if (asynchronous)
        {
            await parser.DisposeAsync();
        }
        else
        {
            parser.Dispose();
        }
        parser.Dispose();
        await parser.DisposeAsync();

        Assert.Equal(1, pool.ReturnCount);
        Assert.Same(pool.Rented, pool.Returned);
        Assert.Throws<ObjectDisposedException>(() => parser.Push(ExampleParser.SyncByte));
        Assert.Throws<ObjectDisposedException>(() => parser.Reset());
        Assert.All(pool.Returned!, item => Assert.Equal(0xAA, item));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(251)]
    [InlineData(252)]
    [InlineData(255)]
    public void Push_MaximumPayloadAndFollowingFrame_ParseWithoutOverflow(int payloadSize)
    {
        var pool = new ExactPool();
        using var parser = CreateParser(pool);
        var messages = 0;
        var lastSize = -1;
        using var subscription = parser.OnMessage.Subscribe(message =>
        {
            messages++;
            lastSize = ((PayloadMessage)message).PayloadSize;
        });
        var frame = new byte[payloadSize + 5];
        frame[0] = ExampleParser.SyncByte;
        frame[1] = 17;
        frame[2] = 99;
        frame[3] = (byte)payloadSize;
        frame.AsSpan(4, payloadSize).Fill(0x5A);
        frame[^1] = ExampleMessageBase.CalcCrc(frame.AsSpan(1, frame.Length - 2));
        foreach (var value in frame)
        {
            parser.Push(value);
        }
        Assert.Equal(1, messages);
        Assert.Equal(payloadSize, lastSize);

        foreach (var value in frame)
        {
            parser.Push(value);
        }
        Assert.Equal(2, messages);
        Assert.Equal((uint)2, parser.Statistic.ParsedMessages);
        Assert.Equal((uint)0, parser.Statistic.DeserializeError);
    }

    private static ExampleParser CreateParser(ArrayPool<byte> pool)
    {
        var factory = new ProtocolMessageFactoryBuilder<ExampleMessageBase, byte>(
            ExampleProtocol.Info
        )
            .Add<PayloadMessage>(99)
            .Build();
        var context = (IProtocolContext)Protocol.Create(_ => { });
        return new ExampleParser(factory, context, null, pool);
    }

    public sealed class PayloadMessage : ExampleMessageBase
    {
        public int PayloadSize { get; private set; }
        public override string Name => nameof(PayloadMessage);
        public override byte Id => 99;

        public override void Accept(IVisitor visitor) { }

        protected override void InternalDeserialize(ref ReadOnlySpan<byte> buffer)
        {
            PayloadSize = buffer.Length;
            buffer = buffer[buffer.Length..];
        }

        protected override void InternalSerialize(ref Span<byte> buffer) { }

        protected override int InternalGetByteSize() => 0;
    }

    private sealed class ExactPool : ArrayPool<byte>
    {
        public byte[]? Rented { get; private set; }
        public byte[]? Returned { get; private set; }
        public int ReturnCount { get; private set; }

        public override byte[] Rent(int minimumLength) => Rented = new byte[minimumLength];

        public override void Return(byte[] array, bool clearArray = false)
        {
            Returned = array;
            ReturnCount++;
            array.AsSpan().Fill(0xAA);
        }
    }
}
