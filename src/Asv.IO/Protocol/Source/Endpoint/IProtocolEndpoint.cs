using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using R3;

namespace Asv.IO;

public interface IProtocolEndpoint:IDisposable, IAsyncDisposable,IProtocolConnection
{
    IEnumerable<IProtocolParser> Parsers { get; }
    ReadOnlyReactiveProperty<bool> IsConnected { get; }
    bool IsDisposed { get; }
    
}