using System.Collections.Generic;
using System.Threading.Tasks;

namespace Asv.Common;

public interface ISupportNavigation<TBase, TId> : ISupportId<TId>
{
    ValueTask<TBase> Navigate(TId id);
}

public static class NavigationMixin
{
    public static async ValueTask<TBase> NavigateByPath<TBase, TId>(
        this TBase src,
        IEnumerable<TId> path
    )
        where TBase : ISupportNavigation<TBase, TId>
    {
        var result = src;
        foreach (var id in path)
        {
            src = await result.Navigate(id);
            result = src;
        }

        return result;
    }
}
