using System;

namespace Asv.IO;

public class ProtocolParserReadNotAllDataWhenDeserializePacketException : ProtocolParserException
{
    public IProtocolMessage ProtocolMessage { get; }
    public ProtocolParserReadNotAllDataWhenDeserializePacketException(ProtocolInfo parser, IProtocolMessage message) 
        : base(parser, $"Read not all data when deserialize '{parser}.{message.Name}' message")
    {
        ProtocolMessage = message;
    }

}