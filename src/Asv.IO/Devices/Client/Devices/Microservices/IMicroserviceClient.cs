using System;
using System.Threading;
using System.Threading.Tasks;

namespace Asv.IO;

public interface IMicroserviceClient: IDisposable, IAsyncDisposable
{
    string Id { get; }
    string Type { get; }
    bool IsInit { get; }
    Task Init(CancellationToken cancel = default);
}

