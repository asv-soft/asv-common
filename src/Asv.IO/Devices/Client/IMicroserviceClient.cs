namespace Asv.IO;

public interface IMicroserviceClient : IMicroservice { }

public abstract class MicroserviceClient<TBaseMessage>(IMicroserviceContext context, string id)
    : MicroserviceBase<TBaseMessage>(context, id),
        IMicroserviceClient
    where TBaseMessage : IProtocolMessage;
