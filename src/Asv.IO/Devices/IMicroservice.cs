using System;
using System.Threading;
using System.Threading.Tasks;

namespace Asv.IO;

public interface IMicroservice: IDisposable, IAsyncDisposable
{
    string Id { get; }
    string TypeName { get; }
    bool IsInit { get; }
    Task Init(CancellationToken cancel = default);
    
}
