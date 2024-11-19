using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Asv.IO;

public class ExampleParser(ImmutableDictionary<ushort, Func<ExampleMessageBase>> messageFactory)
    : ProtocolParserBase<ExampleMessageBase, ushort>(messageFactory)
{
    private readonly ImmutableDictionary<ushort, Func<ExampleMessageBase>> _messageFactory = messageFactory;

    public static readonly ProtocolParserInfo ParserInfo = new("Example", "Example parser", "Example parser fro tests");


    public override ProtocolParserInfo Info => ParserInfo;
    public override bool Push(byte data)
    {
        
    }

    public override void Reset()
    {
        
    }
}

public abstract class ExampleMessageBase : IProtocolMessage<ushort>
{
    public abstract void Deserialize(ref ReadOnlySpan<byte> buffer);
    public abstract void Serialize(ref Span<byte> buffer);
    public abstract int GetByteSize();
    public ProtocolParserInfo ProtocolId => ExampleParser.ParserInfo;
    public ProtocolTags Tags { get; } = new();
    public abstract string Name { get; }
    public string GetIdAsString() => Id.ToString();
    public abstract ushort Id { get; }
}

public class ExampleMessage1: ExampleMessageBase
{
    public const string MessageName = "ExampleMessage1";
    public const int MessageId = 1;
    
    public override void Deserialize(ref ReadOnlySpan<byte> buffer)
    {
        
    }

    public override void Serialize(ref Span<byte> buffer)
    {
        BinSerialize.WritePackedInteger(ref buffer, Value1);
        BinSerialize.WriteUShort(ref buffer, Value2);
        BinSerialize.WriteString(ref buffer, Value3 ?? string.Empty);
    }

    public override int GetByteSize()
    {
        return BinSerialize.GetSizeForPackedInteger(Value1) + sizeof(ushort) + BinSerialize.GetSizeForString(Value3 ?? string.Empty);   
    }

    public override string Name => MessageName;
    public override ushort Id => MessageId;

    public int Value1 { get; set; }
    public ushort Value2 { get; set; }
    public string? Value3 { get; set; }
    
}