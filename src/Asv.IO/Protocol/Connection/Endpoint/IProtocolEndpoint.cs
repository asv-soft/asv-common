using System.Collections.Generic;
using R3;

namespace Asv.IO;

public interface IProtocolEndpoint : IProtocolConnection
{
    IEnumerable<IProtocolParser> Parsers { get; }
    ReadOnlyReactiveProperty<ProtocolConnectionException?> LastError { get; }
    bool IsDisposed { get; }
}
