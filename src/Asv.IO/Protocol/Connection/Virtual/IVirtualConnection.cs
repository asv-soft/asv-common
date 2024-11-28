using System;

namespace Asv.IO;

public interface IVirtualConnection:IDisposable,IAsyncDisposable
{
    IStatistic Statistic { get; }
    IProtocolConnection Server { get; }
    IProtocolConnection Client { get; }
}