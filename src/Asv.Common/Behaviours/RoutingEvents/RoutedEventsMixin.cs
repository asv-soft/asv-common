using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Asv.Common;

public delegate ValueTask RoutedEventHandler<in T, in TEvent>(T owner, TEvent e)
    where T : ISupportRoutedEvents<T>
    where TEvent : AsyncRoutedEvent<T>;

public static class RoutedEventsMixin
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ValueTask RiseBroadcast<TSelf>(
        this ISupportRoutedEvents<TSelf> source,
        AsyncRoutedEvent<TSelf> routedEvent,
        CancellationToken cancel = default
    )
        where TSelf : ISupportRoutedEvents<TSelf>, ISupportParent<TSelf>
    {
        if (routedEvent.Strategy != RoutingStrategy.Tunnel)
        {
            throw new InvalidOperationException(
                "Only tunneling events can be broadcasted to all children"
            );
        }
        return source.GetRoot().Rise(routedEvent, cancel);
    }

    /// <summary>
    /// Triggers the specified asynchronous routed event within the event controller of the source object.
    /// </summary>
    /// <typeparam name="TSelf">The type that implements <see cref="ISupportRoutedEvents{T}"/>.</typeparam>
    /// <param name="source">The source object that triggers the routed event.</param>
    /// <param name="routedEvent">The routed event to be triggered.</param>
    /// <param name="cancel">A cancellation token used to cancel the operation if needed.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation of triggering the event.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ValueTask Rise<TSelf>(
        this ISupportRoutedEvents<TSelf> source,
        AsyncRoutedEvent<TSelf> routedEvent,
        CancellationToken cancel = default
    )
        where TSelf : ISupportRoutedEvents<TSelf>
    {
        return source.Events.Rise(routedEvent, cancel);
    }

    /// <summary>
    /// Subscribes to routed events within the event controller of the source object.
    /// </summary>
    /// <typeparam name="TSelf">The type that implements <see cref="ISupportRoutedEvents{T}"/>.</typeparam>
    /// <param name="source">The source object whose routed events will be monitored.</param>
    /// <param name="handler">The event handler to be invoked when a routed event occurs.</param>
    /// <returns>An <see cref="IDisposable"/> object that can be used to unsubscribe from the routed events.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IDisposable Subscribe<TSelf>(this TSelf source, RoutedEventHandler<TSelf> handler)
        where TSelf : ISupportRoutedEvents<TSelf>
    {
        return source.Events.Subscribe(handler);
    }

    /// <summary>
    /// Subscribes to the routed events of the specified source object using the provided event handler.
    /// </summary>
    /// <typeparam name="TSelf">The type that implements <see cref="ISupportRoutedEvents{T}"/>.</typeparam>
    /// <typeparam name="TEvent"> The specific type of routed event to subscribe to. </typeparam>
    /// <param name="source">The source object to subscribe for routed events.</param>
    /// <param name="handler">The event handler that will be invoked when a routed event is triggered.</param>
    /// <returns>An <see cref="IDisposable"/> object that can be used to unsubscribe from the events.</returns>
    public static IDisposable Subscribe<TSelf, TEvent>(
        this ISupportRoutedEvents<TSelf> source,
        RoutedEventHandler<TSelf, TEvent> handler
    )
        where TSelf : ISupportRoutedEvents<TSelf>
        where TEvent : AsyncRoutedEvent<TSelf>
    {
        return source.Events.Subscribe(
            (owner, @event) =>
            {
                if (@event is TEvent ev)
                {
                    return handler(owner, ev);
                }
                return ValueTask.CompletedTask;
            }
        );
    }
}
