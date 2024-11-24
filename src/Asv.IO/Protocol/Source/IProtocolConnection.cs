using System;
using System.Threading;
using System.Threading.Tasks;
using R3;

namespace Asv.IO;

public interface IProtocolConnection:ISupportTag
{
    string Id { get; }
    IStatistic Statistic { get; }
    ValueTask Send(IProtocolMessage message, CancellationToken cancel = default);
}

