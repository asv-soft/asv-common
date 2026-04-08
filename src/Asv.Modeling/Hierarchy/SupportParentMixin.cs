using Asv.Common;
using ObservableCollections;
using R3;

namespace Asv.Modeling;

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

    public static IDisposable SetParent<TModel, TView, TBase>(
        this ISynchronizedView<TModel, TView> src,
        TBase parent
    )
        where TBase : class, ISupportParent<TBase>
        where TView : class, ISupportParent<TBase>
    {
        src.ForEach(item => item.Parent = parent);
        var sub1 = src.ObserveAdd().Subscribe(x => x.Value.View.Parent = parent);
        var sub2 = src.ObserveRemove().Subscribe(x => x.Value.View.Parent = null);
        return Disposable.Combine(sub1, sub2);
    }

    public static ISynchronizedView<TModel, TView> SetParent<TModel, TView, TBase>(
        this ISynchronizedView<TModel, TView> src,
        TBase parent,
        CompositeDisposable dispose
    )
        where TBase : class, ISupportParent<TBase>
        where TView : class, ISupportParent<TBase>
    {
        src.ForEach(item => item.Parent = parent);
        src.ObserveAdd().Subscribe(x => x.Value.View.Parent = parent).DisposeItWith(dispose);
        src.ObserveRemove().Subscribe(x => x.Value.View.Parent = null).DisposeItWith(dispose);
        return src;
    }

    public static IDisposable SetParent<TModel, TBase>(
        this IObservableCollection<TModel> src,
        TBase parent
    )
        where TBase : class, ISupportParent<TBase>
        where TModel : class, ISupportParent<TBase>
    {
        src.ForEach(item => item.Parent = parent);
        var sub1 = src.ObserveAdd().Subscribe(x => x.Value.Parent = parent);
        var sub2 = src.ObserveRemove().Subscribe(x => x.Value.Parent = null);
        return Disposable.Combine(sub1, sub2);
    }
}
