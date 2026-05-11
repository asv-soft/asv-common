using R3;

namespace Asv.Modeling;

public sealed class PropertyUndoHandler<T> : UndoHandler<ScalarChange<T>>
{
    private readonly ReactiveProperty<T> _property;
    private readonly IDisposable _sub1;

    public PropertyUndoHandler(string name, ReactiveProperty<T> property)
        : base(name)
    {
        _property = property;
        _sub1 = _property
            .Pairwise()
            .Select(x => new ScalarChange<T> { OldValue = x.Previous, NewValue = x.Current })
            .Subscribe(Publish);
    }

    public override IChange Create()
    {
        return new ScalarChange<T>();
    }

    protected override ValueTask InternalUndo(ScalarChange<T> change, CancellationToken cancel)
    {
        _property.Value = change.OldValue;
        return ValueTask.CompletedTask;
    }

    protected override ValueTask InternalRedo(ScalarChange<T> change, CancellationToken cancel)
    {
        _property.Value = change.NewValue;
        return ValueTask.CompletedTask;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _sub1.Dispose();
        }
        base.Dispose(disposing);
    }
}
