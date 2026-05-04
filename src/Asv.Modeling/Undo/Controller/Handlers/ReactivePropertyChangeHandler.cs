using R3;

namespace Asv.Modeling;

public class ReactivePropertyChangeHandler<T>(string name, ReactiveProperty<T> property)
    : UndoHandler<ScalarChange<T>>(
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
