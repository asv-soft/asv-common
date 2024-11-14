using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using R3;

namespace Asv.IO;

public interface IProtocolConnection:IDisposable, IAsyncDisposable
{
    uint StatRxBytes { get; }
    uint StatTxBytes { get; }
    uint StatTxMessages { get; }
    uint StatRxMessages { get; }
    string Id { get; }
    TagList Tags { get; }
    IEnumerable<IProtocolParser> Parsers { get; }
    Observable<IProtocolMessage> OnMessageReceived { get; }
    Observable<IProtocolMessage> OnMessageSent { get; }
    ValueTask Send(IProtocolMessage message, CancellationToken cancel = default);
    bool IsDisposed { get; }
}

public interface IProtocolRouteFilter
{
    int Priority { get; }
    bool OnReceiveFilterAndTransform(ref IProtocolMessage message, ProtocolConnection connection);
    bool OnSendFilterTransform(ref IProtocolMessage message, ProtocolConnection connection);
}   

