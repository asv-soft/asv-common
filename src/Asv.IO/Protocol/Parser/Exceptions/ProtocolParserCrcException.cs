namespace Asv.IO;

public class ProtocolParserCrcException : ProtocolParserException
{
    public ProtocolParserCrcException(ProtocolInfo parser)
        : base(parser, $"Crc error occurred when recv packet {parser}") { }
}
