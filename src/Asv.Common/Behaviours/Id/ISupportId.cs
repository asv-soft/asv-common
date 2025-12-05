using System.Collections.Generic;

namespace Asv.Common;

public interface ISupportId<out T>
{
    T Id { get; }
}

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
}
