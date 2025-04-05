using System.Threading;
using System.Threading.Tasks;

namespace Asv.IO;

public class EndpointIdTagFeature : IProtocolFeature
{
    public string Name => "Endpoint id for message";
    public string Description => "Set tag of endpoint id for every received message";
    public string Id => nameof(EndpointIdTagFeature);
    public int Priority => 0;
    
    public ValueTask<IProtocolMessage?> ProcessRx(IProtocolMessage message, IProtocolConnection connection, CancellationToken cancel)
    {
        if (connection is IProtocolEndpoint endpoint)
        {
            endpoint.SetConnectionTag(message);
        }

        return ValueTask.FromResult<IProtocolMessage?>(message);
    }

    public ValueTask<IProtocolMessage?> ProcessTx(IProtocolMessage message, IProtocolConnection connection, CancellationToken cancel)
    {
        return ValueTask.FromResult<IProtocolMessage?>(message);
    }

    public void Register(IProtocolConnection connection)
    {
        if (connection is IProtocolEndpoint endpoint)
        {
            endpoint.SetConnectionTag();
        }
    }

    public void Unregister(IProtocolConnection connection)
    {
        
    }

    
}