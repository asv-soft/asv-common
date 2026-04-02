using System.Buffers;
using System.Threading;
using System.Threading.Tasks;
using R3;

namespace Asv.Common;

public class ReactivePropertyChangeHandler<T>(string name, ReactiveProperty<T> property)
    : UndoChangeHandler<ScalarChange<T>>(
        name,
        property.Pairwise().Select(x => (IChange)new ScalarChange<T>(x.Previous, x.Current))
    )
{
    public override IChange Create()
    {
        return new ScalarChange<T>();
    }

    protected override ValueTask InternalUndo(ScalarChange<T> change, CancellationToken cancel)
    {
        property.Value = change.OldValue;
        return ValueTask.CompletedTask;
    }

    protected override ValueTask InternalRedo(ScalarChange<T> change, CancellationToken cancel)
    {
        property.Value = change.NewValue;
        return ValueTask.CompletedTask;
    }
}

public class ScalarChange<T> : IChange
{
    public ScalarChange() { }

    public ScalarChange(T previous, T current) { }

    public T OldValue { get; private set; }
    public T NewValue { get; private set; }

    public void Serialize(IBufferWriter<byte> writer)
    {
        throw new System.NotImplementedException();
    }

    public void Deserialize(ReadOnlySequence<byte> data)
    {
        throw new System.NotImplementedException();
    }
}
