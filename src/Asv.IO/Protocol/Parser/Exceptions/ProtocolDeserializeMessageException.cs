using System;

namespace Asv.IO;

public class ProtocolDeserializeMessageException : ProtocolParserException
{
    public ProtocolDeserializeMessageException(ProtocolParserInfo parser, IProtocolMessage message, Exception ex) 
        : base(parser, $"Deserialization {parser}.{message.Name} message error:{ex.Message}",ex)
    {
        
    }
   
}