using Asv.Common;
using R3;

namespace Asv.Modeling;

/// <summary>
/// Provides convenience registration methods for <see cref="ILayoutController"/>.
/// </summary>
public static class LayoutControllerMixin
{
    extension(ILayoutController controller)
    {
        /// <summary>
        /// Registers a layout value with a synchronous load callback.
        /// </summary>
        /// <typeparam name="TData">The value type stored for this layout identifier.</typeparam>
        /// <param name="layoutId">The identifier of the layout value within the owner.</param>
        /// <param name="load">The callback invoked when the layout value is loaded.</param>
        /// <returns>A sink used to load, save, and unregister the layout value.</returns>
        public ILayoutSink<TData> Register<TData>(string layoutId, Action<TData> load)
        {
            ArgumentNullException.ThrowIfNull(load);

            return controller.Register<TData>(
                layoutId,
                (data, _) =>
                {
                    load(data);
                    return ValueTask.CompletedTask;
                }
            );
        }

        /// <summary>
        /// Registers a scalar layout value and saves it whenever the specified trigger produces a value.
        /// </summary>
        /// <typeparam name="TValue">The stored value type.</typeparam>
        /// <typeparam name="TAny">The trigger value type.</typeparam>
        /// <param name="layoutId">The identifier of the layout value within the owner.</param>
        /// <param name="load">The callback invoked when the layout value is loaded.</param>
        /// <param name="save">The function that returns the current value to save.</param>
        /// <param name="trigger">The observable that triggers saving.</param>
        /// <returns>A disposable registration and trigger subscription.</returns>
        public IDisposable Register<TValue, TAny>(
            string layoutId,
            Action<TValue> load,
            Func<TValue?> save,
            Observable<TAny> trigger
        )
        {
            var sink = controller.Register(layoutId, load);
            var sub = trigger.SubscribeAwait(
                (_, c) =>
                {
                    if (save() is { } data)
                    {
                        return sink.SaveAsync(data, c);
                    }
                    return ValueTask.CompletedTask;
                }
            );
            return Disposable.Combine(sink, sub);
        }

        /// <summary>
        /// Loads all registered layout values immediately when a root is already attached,
        /// and again every time the owner is attached to a root.
        /// </summary>
        /// <typeparam name="TRoot">The tracked root type.</typeparam>
        /// <param name="rootTracking">The root tracking controller that provides root attachment notifications.</param>
        /// <param name="cancel">A cancellation token used to cancel layout loading.</param>
        /// <returns>A disposable subscription that stops future layout loads when disposed.</returns>
        public IDisposable LoadWhenRootAttached<TRoot>(
            IRootTrackingController<TRoot> rootTracking,
            CancellationToken cancel = default
        )
        {
            ArgumentNullException.ThrowIfNull(rootTracking);

            return rootTracking.ExecuteWhenRootAttached(_ =>
                controller.LoadAllAsync(cancel).AsTask().SafeFireAndForget()
            );
        }
    }
}
