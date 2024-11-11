using System;
using System.Diagnostics;
using System.IO.Pipelines;

namespace Asv.IO;

public interface IPipeEndpoint : IDuplexPipe, IDisposable,IAsyncDisposable
{
    string Id { get; }
    IPipePort Parent { get; }
    bool IsDisposed { get; }
    TagList Tags { get; }
}

