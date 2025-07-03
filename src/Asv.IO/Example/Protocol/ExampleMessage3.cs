using System;

namespace Asv.IO;

public class ExampleMessage3 : ExampleMessageBase
{
    protected override void InternalDeserialize(ref ReadOnlySpan<byte> buffer)
    {
        
    }

    protected override void InternalSerialize(ref Span<byte> buffer)
    {
        
    }

    protected override int InternalGetByteSize()
    {
        return 0;
    }

    public override string Name => "Empty";
    public override byte Id => 3;
    public override void Accept(IVisitor visitor)
    {
        
    }
}