namespace Asv.IO;

public interface IProtocolProcessingFeature
{
    int Priority { get; }
    bool OnReceiveFilterAndTransform(ref IProtocolMessage message, IProtocolConnection connection);
    bool OnSendFilterTransform(ref IProtocolMessage message, IProtocolConnection connection);
}

public class IgnoreReceievedProcessingFeature : IProtocolProcessingFeature
{
    public int Priority { get; } = 0;
    public bool OnReceiveFilterAndTransform(ref IProtocolMessage message, IProtocolConnection connection)
    {
        message.Tags.SetConnectionId(connection.Id);
        return true;
    }

    public bool OnSendFilterTransform(ref IProtocolMessage message, IProtocolConnection connection)
    {
        if (message.Tags.GetConnectionId() == connection.Id)
        {
            return false;
        }

        return true;
    }
}