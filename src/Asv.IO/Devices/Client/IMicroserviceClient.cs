namespace Asv.IO;

public interface IMicroserviceClient : IMicroservice
{
    
}

public abstract class MicroserviceClient<TBaseMessage> : MicroserviceBase<TBaseMessage>, IMicroserviceClient
    where TBaseMessage : IProtocolMessage
{
    protected MicroserviceClient(IDeviceContext context, string id) : base(context, id)
    {
        
    }
}