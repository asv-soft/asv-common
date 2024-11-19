using System.Threading;
using System.Threading.Tasks;

namespace Asv.IO;

public class MessageBroadcastingFeature : IProtocolProcessingFeature
{
    public const string FeatureId = "Broadcasting";
    
    private static readonly ValueTask<bool> TrueTask = ValueTask.FromResult(true);
    private static readonly ValueTask<bool> FalseTask = ValueTask.FromResult(false);
    public string Name => "Message Broadcasting";
    public string Description => "Allows retransmission of incoming messages to all other connections.";
    public string Id => FeatureId;
    public int Priority { get; } = 0;
    public ValueTask<bool> ProcessReceiveMessage(ref IProtocolMessage message, IProtocolConnection connection, CancellationToken cancel)
    {
        // mark message with connection id
        message.Tags.SetConnectionId(connection.Id);
        return TrueTask;
    }

    public ValueTask<bool> ProcessSendMessage(ref IProtocolMessage message, IProtocolConnection connection, CancellationToken cancel)
    {
        // check if message was received by this connection => skip it
        return message.Tags.GetConnectionId() == connection.Id ? FalseTask : TrueTask;
    }

    public ValueTask<bool> ProcessSendMessage(ref IProtocolMessage message, IProtocolRouter router, CancellationToken cancel)
    {
        throw new System.NotImplementedException();
    }

    public ValueTask<bool> ProcessReceivedMessage(ref IProtocolMessage message, IProtocolRouter router, CancellationToken cancel)
    {
        // all received messages broadcast to all other connections
        router.Send(message, cancel);
        return TrueTask;
    }

}