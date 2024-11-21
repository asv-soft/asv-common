using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using R3;

namespace Asv.IO;

public interface IProtocolEndpoint:IDisposable, IAsyncDisposable,IProtocolConnection, ISupportTag
{
    string Id { get; }
    uint StatRxBytes { get; }
    uint StatTxBytes { get; }
    uint StatRxMessages { get; }
    uint StatTxMessages { get; }
    IEnumerable<IProtocolParser> Parsers { get; }
    ReadOnlyReactiveProperty<bool> IsConnected { get; }
    bool IsDisposed { get; }
    
}