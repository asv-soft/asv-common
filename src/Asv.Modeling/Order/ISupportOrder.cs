namespace Asv.Modeling;

public interface ISupportOrder
{
    int Order { get; }
}

public class SupportOrderComparer : IComparer<ISupportOrder>
{
    public static IComparer<ISupportOrder> Instance { get; } = new SupportOrderComparer();

    public int Compare(ISupportOrder? x, ISupportOrder? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (y is null) return 1;
        if (x is null) return -1;
        return x.Order.CompareTo(y.Order);
    }
}