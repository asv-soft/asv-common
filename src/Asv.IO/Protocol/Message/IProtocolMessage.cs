namespace Asv.IO;

public interface IProtocolMessage:ISizedSpanSerializable, ISupportTag
{
    ProtocolInfo Protocol { get; }
    string Name { get; }
    string GetIdAsString();
}

public interface IProtocolMessage<out TMessageId> : IProtocolMessage
{
    TMessageId Id { get; }
}