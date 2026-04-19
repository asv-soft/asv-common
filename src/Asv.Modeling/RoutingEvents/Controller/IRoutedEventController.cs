namespace Asv.Modeling;

public delegate ValueTask RoutedEventHandler<T>(T owner, AsyncRoutedEvent<T> e, CancellationToken cancel)
    where T : ISupportRoutedEvents<T>;

public delegate ValueTask RoutedEventHandler<in T, in TEvent>(T owner, TEvent e, CancellationToken cancel)
    where T : ISupportRoutedEvents<T>
    where TEvent : AsyncRoutedEvent<T>;

public interface IRoutedEventController<TBase>
    where TBase : ISupportRoutedEvents<TBase>
{
    TBase Owner { get; }
    ValueTask Rise(AsyncRoutedEvent<TBase> routedEvent, CancellationToken cancel = default);
    IDisposable Catch(RoutedEventHandler<TBase> handler);
    IDisposable Catch<TEvent>(RoutedEventHandler<TBase, TEvent> handler)
        where TEvent : AsyncRoutedEvent<TBase>;
    IDisposable Catch<TEvent>(Action<TEvent> handler)
        where TEvent : AsyncRoutedEvent<TBase>;
}
