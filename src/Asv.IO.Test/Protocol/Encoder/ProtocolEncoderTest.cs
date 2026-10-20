using System;
using System.Collections.Generic;
using Xunit;
using static Asv.IO.Test.Encoder.EncoderTestSupport;

namespace Asv.IO.Test.Encoder;

public class ProtocolEncoderTest
{
    [Fact]
    public void DefaultEncoder_PreservesExistingExampleProtocolBytes()
    {
        var message = new ExampleMessage1
        {
            SenderId = 7,
            Value1 = 123,
            Value3 = "legacy",
        };
        var expected = message.Serialize();
        var encoder = DefaultProtocolEncoder.Instance;
        var actual = new byte[encoder.GetMaxByteSize(message)];

        var written = encoder.Encode(message, actual);

        Assert.Equal(expected.Length, written);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(8)]
    public void DefaultEncoder_ReturnsConsumedLengthAndSerializesOnce(int maximumSize)
    {
        var message = new TestMessage(42, maximumSize);
        var destination = new byte[maximumSize + 3];
        Array.Fill(destination, (byte)0xCC);

        var written = DefaultProtocolEncoder.Instance.Encode(message, destination);

        Assert.Equal(1, written);
        Assert.Equal(42, destination[0]);
        Assert.All(destination[1..], value => Assert.Equal(0xCC, value));
        Assert.Equal(1, message.SerializeCalls);
    }

    [Fact]
    public void DefaultEncoder_SizeAndInsufficientBufferDoNotSerialize()
    {
        var message = new TestMessage(42, 8);
        var encoder = DefaultProtocolEncoder.Instance;

        Assert.Equal(8, encoder.GetMaxByteSize(message));
        Assert.Equal(8, encoder.GetMaxByteSize(message));
        Assert.Throws<ArgumentException>(() => encoder.Encode(message, new byte[7]));
        Assert.Equal(0, message.SerializeCalls);
    }

    [Fact]
    public void CreateEncoder_LegacyContextFallsBackToExistingSerialization()
    {
        IProtocolContext context = new LegacyContext(CreateContext(_ => new StatefulEncoder()));
        var encoder = context.CreateEncoder(Info.Id);
        var message = new TestMessage(42, 8);
        var buffer = new byte[8];

        Assert.Same(DefaultProtocolEncoder.Instance, encoder);
        Assert.Equal(1, encoder.Encode(message, buffer));
        Assert.Equal(42, buffer[0]);
        Assert.Equal(1, message.SerializeCalls);
    }

    [Fact]
    public void CreateEncoder_FactoryReceivesContextAndCreatesIndependentInstances()
    {
        IProtocolContext? receivedContext = null;
        var context = CreateContext(current =>
        {
            receivedContext = current;
            return new StatefulEncoder();
        });

        var first = context.CreateEncoder(Info.Id);
        var second = context.CreateEncoder(Info.Id);

        Assert.Same(context, receivedContext);
        Assert.NotSame(first, second);
        Assert.Same(DefaultProtocolEncoder.Instance, context.CreateEncoder("unregistered"));
    }

    [Fact]
    public void LegacyParserBuilder_KeepsRegistrationContractWithoutEncoderMembers()
    {
        var builder = new LegacyParserBuilder();

        builder.RegisterExampleProtocol();

        Assert.Contains(ExampleProtocol.Info.Id, builder.RegisteredProtocols);
        Assert.Throws<NotSupportedException>(() =>
            builder.RegisterEncoder(Info, _ => new StatefulEncoder())
        );
        Assert.Single(builder.RegisteredProtocols);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ClearRegistrations_RemovesAssociatedEncoders(bool clearAll)
    {
        var factory = Protocol.Create(builder =>
        {
            builder.Protocols.Register(Info, (_, _) => new TestParser());
            builder.Protocols.RegisterEncoder(Info, _ => new StatefulEncoder());
            if (clearAll)
            {
                builder.ClearAll();
            }
            else
            {
                builder.Protocols.Clear();
            }
        });
        var context = (IProtocolContext)factory;

        Assert.Empty(context.ParserFactory);
        Assert.Same(DefaultProtocolEncoder.Instance, context.CreateEncoder(Info.Id));
    }

    [Fact]
    public void ModelSerializationAndClone_DoNotConsumeEncoderState()
    {
        var encoder = new StatefulEncoder();
        var message = new TestMessage(42);

        var clone = message.BinaryClone();
        var canonical = message.Serialize();
        Assert.Equal(1, message.GetByteSize());
        Assert.Equal(8, encoder.GetMaxByteSize(message));
        Assert.Equal(0, encoder.EncodeCalls);
        Assert.Equal(message.Payload, clone.Payload);
        Assert.Equal(new byte[] { 42 }, canonical);

        var wire = new byte[8];
        Assert.Equal(2, encoder.Encode(message, wire));
        Assert.Equal(new byte[] { 0, 42 }, wire[..2]);
    }

    private sealed class LegacyParserBuilder : IProtocolParserBuilder
    {
        public List<string> RegisteredProtocols { get; } = [];

        public void Clear() => RegisteredProtocols.Clear();

        public void Register(ProtocolInfo info, ParserFactoryDelegate factory) =>
            RegisteredProtocols.Add(info.Id);
    }
}
