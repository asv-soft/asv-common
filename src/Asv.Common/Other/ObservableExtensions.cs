using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;

namespace Asv.Common
{
    public static class ObservableExtensions
    {
        public static IObservable<T> IgnoreObserverExceptions<T, TException>(
            this IObservable<T> source
        )
            where TException : Exception
        {
            return Observable.Create<T>(o =>
                source.Subscribe(
                    v =>
                    {
                        try
                        {
                            o.OnNext(v);
                        }
                        catch (TException) { }
                    },
                    ex => o.OnError(ex),
                    () => o.OnCompleted()
                )
            );
        }

        public static IObservable<T> IgnoreObserverExceptions<T>(this IObservable<T> source)
        {
            return Observable.Create<T>(o =>
                source.Subscribe(
                    v =>
                    {
                        try
                        {
                            o.OnNext(v);
                        }
                        catch (Exception)
                        {
                            Debug.Fail("Exception ignored");
                        }
                    },
                    o.OnError,
                    o.OnCompleted
                )
            );
        }

        public static IObservable<T> SubscribeEx<T>(
            this IObservable<T> src,
            IObserver<T> observer,
            CancellationToken cancel
        )
        {
            src.Subscribe(observer, cancel);
            return src;
        }

        /// <summary>
        /// Скользящее окно
        /// </summary>
        /// <typeparam name="T">.</typeparam>
        /// <param name="this">.</param>
        /// <param name="buffering">.</param>
        /// <returns></returns>
        public static IObservable<T[]> RollingBuffer<T>(
            this IObservable<T> @this,
            TimeSpan buffering
        )
        {
            return Observable.Create<T[]>(o =>
            {
                var list = new LinkedList<Timestamped<T>>();
                return @this
                    .Timestamp()
                    .Subscribe(
                        tx =>
                        {
                            list.AddLast(tx);
                            while (
                                list.First is not null
                                && list.First.Value.Timestamp < DateTime.Now.Subtract(buffering)
                            )
                            {
                                list.RemoveFirst();
                            }

                            o.OnNext(list.Select(tx2 => tx2.Value).ToArray());
                        },
                        o.OnError,
                        o.OnCompleted
                    );
            });
        }
    }
}
