using System.Collections.Generic;

namespace Asv.Common;

public interface ISupportParent<T>
    where T : ISupportParent<T>
{
    T? Parent { get; set; }
}

public static class SupportParentMixin
{
    public static T GetRoot<T>(this ISupportParent<T> src)
        where T : ISupportParent<T>
    {
        var root = src;
        while (root.Parent != null)
        {
            root = root.Parent;
        }

        return (T)root;
    }

    public static IEnumerable<ISupportParent<T>> GetHierarchyFromRoot<T>(this T src)
        where T : ISupportParent<T>
    {
        if (src.Parent != null)
        {
            foreach (var ancestor in src.Parent.GetHierarchyFromRoot())
            {
                yield return ancestor;
            }
        }

        yield return src;
    }
}
