using System.Runtime.CompilerServices;

namespace Asv.Modeling;



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
}
