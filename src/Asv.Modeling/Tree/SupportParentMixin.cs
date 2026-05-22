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
        where TView : class, ISupportParentChange<TBase>
    {
        src.ForEach(item => item.SetParent(parent));
        var sub1 = src.ObserveAdd().Subscribe(x => x.Value.View.SetParent(parent));
        var sub2 = src.ObserveRemove().Subscribe(x => x.Value.View.SetParent(null));
        return Disposable.Combine(sub1, sub2);
    }

    public static ISynchronizedView<TModel, TView> SetParent<TModel, TView, TBase>(
        this ISynchronizedView<TModel, TView> src,
        TBase parent,
        CompositeDisposable dispose
    )
        where TBase : class, ISupportParent<TBase>
        where TView : class, ISupportParentChange<TBase>
    {
        src.ForEach(item => item.SetParent(parent));
        src.ObserveAdd().Subscribe(x => x.Value.View.SetParent(parent)).DisposeItWith(dispose);
        src.ObserveRemove().Subscribe(x => x.Value.View.SetParent(null)).DisposeItWith(dispose);
        return src;
    }

    public static IDisposable SetParent<TModel, TBase>(
        this IObservableCollection<TModel> src,
        TBase parent
    )
        where TBase : class, ISupportParent<TBase>
        where TModel : class, ISupportParentChange<TBase>
    {
        src.ForEach(item => item.SetParent(parent));
        var sub1 = src.ObserveAdd().Subscribe(x => x.Value.SetParent(parent));
        var sub2 = src.ObserveRemove().Subscribe(x => x.Value.SetParent(null));
        return Disposable.Combine(sub1, sub2);
    }
}
