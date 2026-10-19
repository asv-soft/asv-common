using System.Diagnostics.CodeAnalysis;
namespace Asv.IO;

[SuppressMessage("Design", "RCS1194:Implement exception constructors", Justification = "Protocol context is required to construct this exception.")]
public class ProtocolParserCrcException : ProtocolParserException
{
    public ProtocolParserCrcException(ProtocolInfo parser)
        : base(parser, $"Crc error occurred when recv packet {parser}") { }
}
