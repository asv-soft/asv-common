using System.Threading;
using System.Threading.Tasks;

namespace Asv.IO;

public class BroadcastingFeature : IProtocolFeature
{
    public const string FeatureId = "Broadcasting";
    
    public string Name => "Message Broadcasting";
    public string Description => "Allows retransmission of incoming messages to all other connections.";
    public string Id => FeatureId;
    public int Priority { get; } = 0;
    public async ValueTask<IProtocolMessage?> ProcessRx(IProtocolMessage message, IProtocolConnection connection, CancellationToken cancel)
    {
        if (connection is IProtocolEndpoint endpoint)
        {
            // mark message with connection id
            message.SetConnectionId(endpoint.Id);
        }

        if (connection is IProtocolRouter router)
        {
            // all received messages broadcast to all other connections
            await router.Send(message, cancel);
        }
        return message;
    }

    public ValueTask<IProtocolMessage?> ProcessTx(IProtocolMessage message, IProtocolConnection connection, CancellationToken cancel)
    {
        if (connection is IProtocolEndpoint endpoint)
        {
            // check if message was received by this connection => skip it
            return message.GetConnectionId() == endpoint.Id ? default : ValueTask.FromResult<IProtocolMessage?>(message);
        }
        return ValueTask.FromResult<IProtocolMessage?>(message);
    }

    public void Register(IProtocolConnection connection)
    {
        
    }

    public void Unregister(IProtocolConnection connection)
    {
        
    }


}