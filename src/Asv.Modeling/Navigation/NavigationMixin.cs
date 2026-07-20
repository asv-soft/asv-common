namespace Asv.Modeling;

public static class NavigationMixin
{
    public static ValueTask<TBase> NavigateByPath<TBase>(
        this TBase src,
        NavPath path,
        CancellationToken cancel = default
    )
        where TBase : ISupportNavigation<TBase>
    {
        cancel.ThrowIfCancellationRequested();
        if (path.Count == 0)
        {
            return ValueTask.FromResult(src);
        }

        return NavigateByPathCore(src, path, cancel);
    }

    private static async ValueTask<TBase> NavigateByPathCore<TBase>(
        TBase current,
        NavPath path,
        CancellationToken cancel
    )
        where TBase : ISupportNavigation<TBase>
    {
        for (var i = 0; i < path.Count; i++)
        {
            cancel.ThrowIfCancellationRequested();
            current = await current.Navigate(path[i], cancel).ConfigureAwait(false);
        }

        return current;
    }
}
