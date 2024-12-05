namespace Asv.IO;

public interface IMicroserviceServer : IMicroservice
{
    
}

public abstract class MicroserviceServer<TBaseMessage>(IMicroserviceContext context, string id)
    : MicroserviceBase<TBaseMessage>(context, id), IMicroserviceServer
    where TBaseMessage : IProtocolMessage;