namespace Asv.Store;

public interface IArrayPart<TRow>
{
    ValueTask Read(Action<TRow> visitor, CancellationToken cancel = default);
    ValueTask Write(IEnumerable<TRow> values, CancellationToken cancel = default);
}

public static class ArrayPartMixin
{
    public static ValueTask Read<TRow>(
        this IArrayPart<TRow> src,
        ICollection<TRow> collection,
        CancellationToken cancel = default
    )
    {
        return src.Read(collection.Add, cancel);
    }
}
