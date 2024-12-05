namespace Asv.IO;

public interface IMicroserviceServer : IMicroservice
{
    
}

public abstract class MicroserviceServer<TBaseMessage> : MicroserviceBase<TBaseMessage>, IMicroserviceServer
    where TBaseMessage : IProtocolMessage
{
    protected MicroserviceServer(IDeviceContext context, string id) : base(context, id)
    {
        
    }
}