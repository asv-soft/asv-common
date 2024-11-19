using System;

namespace Asv.IO;

public class ProtocolDeserializeMessageException : ProtocolParserException
{
    public ProtocolDeserializeMessageException(ProtocolInfo parser, IProtocolMessage message, Exception ex) 
        : base(parser, $"Deserialization {parser}.{message.Name} message error:{ex.Message}",ex)
    {
        
    }
    
    public ProtocolDeserializeMessageException(ProtocolInfo parser, IProtocolMessage message, string messageError) 
        : base(parser, $"Deserialization {parser}.{message.Name} message error:{messageError}")
    {
        
    }
   
}