namespace Asv.IO;

public class ProtocolParserUnknownMessageException : ProtocolParserException
{
    public ProtocolParserUnknownMessageException(ProtocolParserInfo parser, object? messageId) 
        : base(parser, $"Unknown message '{parser}.MSG_ID={messageId}'")
    {
        
    }
   
}