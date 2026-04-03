using System.Buffers;
using System.Threading;
using System.Threading.Tasks;
using MemoryPack;
using R3;

namespace Asv.Common;

public class ReactivePropertyChangeHandler<T>(string name, ReactiveProperty<T> property)
    : UndoChangeHandler<ScalarChange<T>>(
        name,
        property
            .Pairwise()
            .Select(x =>
                (IChange)new ScalarChange<T> { OldValue = x.Previous, NewValue = x.Current }
            )
    )
{
    public override IChange Create()
    {
        return new ScalarChange<T> { OldValue = default, NewValue = default };
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

[MemoryPackable]
public partial class ScalarChange<T> : IChange
{
    public T OldValue { get; set; }
    public T NewValue { get; set; }

    public void Serialize(IBufferWriter<byte> writer)
    {
        MemoryPackSerializer.Serialize(writer, this);
    }

    public void Deserialize(ReadOnlySequence<byte> data)
    {
        var src = this;
        MemoryPackSerializer.Deserialize(in data, ref src);
    }
}
