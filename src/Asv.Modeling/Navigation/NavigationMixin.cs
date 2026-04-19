namespace Asv.Modeling;

public static class NavigationMixin
{
    public static ValueTask<TBase> NavigateByPath<TBase>(
        this TBase src,
        NavPath path
    )
        where TBase : ISupportNavigation<TBase>
    {
        if (path.Count == 0)
        {
            return ValueTask.FromResult(src);
        }

        return NavigateByPathCore(src, path);
    }

    private static async ValueTask<TBase> NavigateByPathCore<TBase>(TBase current, NavPath path)
        where TBase : ISupportNavigation<TBase>
    {
        for (var i = 0; i < path.Count; i++)
        {
            current = await current.Navigate(path[i]).ConfigureAwait(false);
        }

        return current;
    }
}