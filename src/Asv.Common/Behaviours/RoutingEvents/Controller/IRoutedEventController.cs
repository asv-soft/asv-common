using System;
using System.Threading;
using System.Threading.Tasks;

namespace Asv.Common;

public delegate ValueTask RoutedEventHandler<T>(T owner, AsyncRoutedEvent<T> e)
    where T : ISupportRoutedEvents<T>;

public interface IRoutedEventController<T>
    where T : ISupportRoutedEvents<T>
{
    T Owner { get; }
    ValueTask Rise(AsyncRoutedEvent<T> routedEvent, CancellationToken cancel = default);
    IDisposable Subscribe(RoutedEventHandler<T> handler);
}
