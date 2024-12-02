using System.Threading;
using System.Threading.Tasks;

namespace Asv.IO;

public interface IProtocolFeature
{
    string Name { get; }
    string Description { get; }
    string Id { get; }
    int Priority { get; }
    ValueTask<IProtocolMessage?> ProcessRx(IProtocolMessage message, IProtocolConnection connection, CancellationToken cancel);
    ValueTask<IProtocolMessage?> ProcessTx(IProtocolMessage message, IProtocolConnection connection, CancellationToken cancel);
    void Register(IProtocolConnection connection);
    void Unregister(IProtocolConnection connection);
}

public interface IProtocolFeatureBuilder
{
    void Clear();
    void Register(IProtocolFeature feature);
}

public static class ProtocolProcessingFeatureHelper
{
    public static void RegisterBroadcastFeature<TMessage>(this IProtocolFeatureBuilder builder)
    {
        builder.Register(new BroadcastingFeature<TMessage>());
    }
    public static void RegisterBroadcastAllFeature(this IProtocolFeatureBuilder builder)
    {
        builder.Register(new BroadcastingFeature<IProtocolMessage>());
    }
}