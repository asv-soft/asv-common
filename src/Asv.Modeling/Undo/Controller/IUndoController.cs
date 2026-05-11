using System.Buffers;

namespace Asv.Modeling;

public interface IChange
{
    void Serialize(IBufferWriter<byte> writer);
    void Deserialize(ReadOnlySequence<byte> data);
}

public enum ChangeOperation : byte
{
    Update = 0,
    Create = 1,
    Read = 2,
    Delete = 3,
}

public interface IChange<T> : IChange
{
    ChangeOperation Operation { get; set; }
    T OldValue { get; set; }
    T NewValue { get; set; }
}

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

public interface IUndoPublisher<in T> : IDisposable
    where T : IChange
{
    void Publish(T change);
}
