using System;
using System.Reactive.Disposables;

namespace Asv.Common
{
    public static class DisposableHelper
    {
        public static T DisposeItWith<T>(this T src, CompositeDisposable disposable)
            where T:IDisposable
        {
            disposable.Add(src);
            return src;
        }

        public static CompositeDisposable AddAction(this CompositeDisposable src, Action dispose)
        {
            src.Add(Disposable.Create(dispose));
            return src;
        }
    }
}
