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
            _ => null
        };
    }

    public ProtocolInfo Info => ExampleProtocol.Info;
   
}