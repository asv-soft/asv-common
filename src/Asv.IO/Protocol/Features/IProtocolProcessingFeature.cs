using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Asv.IO;

public interface IProtocolProcessingFeature
{
    string Name { get; }
    string Description { get; }
    string Id { get; }
    int Priority { get; }
    ValueTask<bool> ProcessReceiveMessage(ref IProtocolMessage message, IProtocolConnection connection, CancellationToken cancel);
    ValueTask<bool> ProcessSendMessage(ref IProtocolMessage message, IProtocolConnection connection, CancellationToken cancel);
    ValueTask<bool> ProcessSendMessage(ref IProtocolMessage message, IProtocolRouter router, CancellationToken cancel);
    ValueTask<bool> ProcessReceivedMessage(ref IProtocolMessage message, IProtocolRouter router, CancellationToken cancel);
}

public interface IProtocolProcessingFeatureFactory
{
    IReadOnlySet<string> SupportedFeatures { get; }
    IProtocolProcessingFeature Create(string id);
}