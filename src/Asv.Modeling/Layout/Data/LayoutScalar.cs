namespace Asv.Modeling;

public sealed class LayoutScalar<T> : ILayoutData
{
    public T Value { get; set; } = default!;
}
