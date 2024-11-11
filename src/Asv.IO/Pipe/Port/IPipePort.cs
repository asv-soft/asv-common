using System;
using System.Diagnostics;
using System.Threading.Tasks;
using ObservableCollections;

namespace Asv.IO;

public interface IPipePort: IDisposable, IAsyncDisposable
{
    TagList Tags { get; }
    IReadOnlyObservableList<IPipeEndpoint> Pipes { get; }
}

public abstract class PipePort:IPipePort
{
    private readonly ObservableList<IPipeEndpoint> _pipes;
    protected PipePort(IPipeCore core)
    {
        Tags = [];
        _pipes = [];
    }
    public TagList Tags { get; }
    public IReadOnlyObservableList<IPipeEndpoint> Pipes => _pipes;

    protected void InternalAddPipe(IPipeEndpoint pipe)
    {
        _pipes.Add(pipe);
    }
    
    #region Dispose

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            // TODO release managed resources here
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual async ValueTask DisposeAsyncCore()
    {
        // TODO release managed resources here
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore();
        GC.SuppressFinalize(this);
    }

    #endregion
}

public class MultiplePipePort : PipePort
{
    public MultiplePipePort(IPipeCore core) : base(core)
    {
        
    }
}