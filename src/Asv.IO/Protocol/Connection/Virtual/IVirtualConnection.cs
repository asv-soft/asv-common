using System;

namespace Asv.IO;

public interface IVirtualConnection:IDisposable,IAsyncDisposable
{
    IProtocolConnection Server { get; }
    IProtocolConnection Client { get; }
}