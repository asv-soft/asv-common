using System.Threading;
using System.Threading.Tasks;

namespace Asv.IO;

public class BroadcastingFeature<TMessage> : IProtocolFeature
{
    public const string FeatureId = $"Broadcasting {nameof(TMessage)}";
    
    public string Name => $"Message Broadcasting {nameof(TMessage)}";
    public string Description => "Allows retransmission of incoming messages to all other connection endpoints.";
    public string Id => FeatureId;
    public int Priority => 0;

    public async ValueTask<IProtocolMessage?> ProcessRx(IProtocolMessage message, IProtocolConnection connection, CancellationToken cancel)
    {
        if (message is not TMessage) return message;
        if (connection is IProtocolEndpoint endpoint)
        {
            // mark message with connection id
            message.MarkBroadcast(connection);
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
        if (connection is IProtocolEndpoint endpoint && message is TMessage)
        {
            // check if message was received by that connection => skip it
            return message.CheckBroadcast(endpoint) ? default : ValueTask.FromResult<IProtocolMessage?>(message);
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