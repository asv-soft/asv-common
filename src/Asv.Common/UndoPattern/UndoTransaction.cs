using System;
using System.Threading;
using System.Threading.Tasks;

namespace Asv.Common;

public class UndoTransaction(string displayName, IUndoContextResolver resolver)
    : DisposableOnce,
        IUndoTransaction
{
    protected override void InternalDisposeOnce()
    {
        throw new NotImplementedException();
    }

    public ValueTask DisposeAsync()
    {
        throw new NotImplementedException();
    }

    public string DisplayName { get; }

    public void Add(IUndoOperation command)
    {
        throw new NotImplementedException();
    }

    public ValueTask Commit(CancellationToken cancel = default)
    {
        throw new NotImplementedException();
    }
}
