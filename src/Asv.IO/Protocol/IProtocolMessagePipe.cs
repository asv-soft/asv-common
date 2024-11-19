using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using R3;

namespace Asv.IO;

public interface IProtocolMessagePipe
{
    Observable<IProtocolMessage> OnMessageReceived { get; }
    Observable<IProtocolMessage> OnMessageSent { get; }
    ValueTask Send(IProtocolMessage message, CancellationToken cancel = default);
}