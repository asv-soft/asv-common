using System.Diagnostics.CodeAnalysis;
namespace Asv.IO;

[SuppressMessage("Design", "RCS1194:Implement exception constructors", Justification = "Protocol context is required to construct this exception.")]
public class ProtocolParserReadNotAllDataWhenDeserializePacketException : ProtocolParserException
{
    public IProtocolMessage ProtocolMessage { get; }

    public ProtocolParserReadNotAllDataWhenDeserializePacketException(
        ProtocolInfo parser,
        IProtocolMessage message
    )
        : base(parser, $"Read not all data when deserialize '{parser}.{message.Name}' message")
    {
        ProtocolMessage = message;
    }
}
