namespace Asv.IO;

public class ProtocolParserCrcException : ProtocolParserException
{
    public ProtocolParserCrcException(ProtocolParserInfo parser) 
        : base(parser, $"Crc error occurred when recv packet {parser}")
    {
    
    }

}