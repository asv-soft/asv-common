namespace Asv.Modeling;

public static class SupportIdMixin
{
    public static IEnumerable<TId> GetPathFromRoot<T, TId>(this T src)
        where T : ISupportParent<T>, ISupportId<TId>
    {
        var current = src;
        var stack = new Stack<TId>();

        while (current != null)
        {
            stack.Push(current.Id);
            current = current.Parent;
        }

        return stack;
    }

    public static IEnumerable<TId> GetPathFrom<T, TId>(this T src, T parent)
        where T : ISupportParent<T>, ISupportId<TId>
    {
        if (src.Equals(parent))
        {
            return [];
        }
        var current = src;
        var stack = new Stack<TId>();

        while (!current.Equals(parent))
        {
            stack.Push(current.Id);
            if (current.Parent == null)
            {
                throw new Exception("Parent not found");
            }
            current = current.Parent;
        }

        return stack;
    }
}