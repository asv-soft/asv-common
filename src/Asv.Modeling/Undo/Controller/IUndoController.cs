using System.Buffers;

namespace Asv.Modeling;

public delegate ValueTask UndoCallback<in TChange>(TChange change, CancellationToken cancel)
    where TChange : IChange;

public interface IUndoController : IDisposable
{
    IUndoPublisher<TChange> Create<TChange>(
        string registrationId,
        UndoCallback<TChange> undo,
        UndoCallback<TChange> redo,
        Func<TChange> factory
    )
        where TChange : IChange;

    IUndoHandler this[string registrationId] { get; }

    IDisposable SuppressChangePublication();
}
