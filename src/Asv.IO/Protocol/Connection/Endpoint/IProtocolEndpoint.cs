using System.Collections.Generic;
using R3;

namespace Asv.IO;

public interface IProtocolEndpoint: IProtocolConnection
{
    IEnumerable<IProtocolParser> Parsers { get; }
    ReadOnlyReactiveProperty<bool> IsConnected { get; }
    bool IsDisposed { get; }
    
}