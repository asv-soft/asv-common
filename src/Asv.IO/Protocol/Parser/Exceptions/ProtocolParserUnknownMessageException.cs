using System.Diagnostics.CodeAnalysis;
namespace Asv.IO;

[SuppressMessage("Design", "RCS1194:Implement exception constructors", Justification = "Protocol context is required to construct this exception.")]
public class ProtocolParserUnknownMessageException : ProtocolParserException
{
    public ProtocolParserUnknownMessageException(ProtocolInfo parser, object? messageId)
        : base(parser, $"Unknown message '{parser}.MSG_ID={messageId}'") { }
}
