using System;
using System.Collections.Generic;

namespace Asv.IO;

public class ExampleMessageFactory:IProtocolMessageFactory<ExampleMessageBase,byte>
{
    public static ExampleMessageFactory Instance { get; } = new();
    private ExampleMessageFactory()
    {
        
    }
    public ExampleMessageBase? Create(byte id)
    {
        return id switch
        {
            ExampleMessage1.MessageId => new ExampleMessage1(),
            ExampleMessage2.MessageId => new ExampleMessage2(),
            _ => null
        };
    }

    public ProtocolInfo Info => ExampleProtocol.Info;
   
}