namespace Asv.IO;

public class ProtocolParserUnknownMessageException : ProtocolParserException
{
    public ProtocolParserUnknownMessageException(ProtocolInfo parser, object? messageId) 
        : base(parser, $"Unknown message '{parser}.MSG_ID={messageId}'")
    {
        
    }
   
}