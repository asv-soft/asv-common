using System;

namespace Asv.IO;

public class ProtocolDeserializeMessageException : ProtocolParserException
{
    public ProtocolDeserializeMessageException(ProtocolInfo parser, IProtocolMessage message, Exception ex) 
        : this(parser, message.Name,ex)
    {
        
    }
    
    public ProtocolDeserializeMessageException(ProtocolInfo parser, string messageName, Exception ex) 
        : base(parser, $"Deserialization {parser}.{messageName} message error.",ex)
    {
        
    }
    
    public ProtocolDeserializeMessageException(ProtocolInfo parser, IProtocolMessage message, string messageError) 
        : this(parser, message.Name, messageError)
    {
        
    }
    
    public ProtocolDeserializeMessageException(ProtocolInfo parser, string messageName, string messageError) 
        : base(parser, $"Deserialization {parser}.{messageName} message error:{messageError}")
    {
        
    }
   
}