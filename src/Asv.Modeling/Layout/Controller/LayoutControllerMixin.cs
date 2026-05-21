using R3;

namespace Asv.Modeling;

public static class LayoutControllerMixin
{
    extension(ILayoutController controller)
    {
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

        public IDisposable Register<TValue, TAny>(
            string layoutId,
            Action<TValue> load,
            Func<TValue?> save,
            Observable<TAny> trigger
        )
        {
            var sink = controller.Register<TValue>(layoutId, load);
            var sub = trigger.Subscribe(_ =>
            {
                if (save() is { } data)
                {
                    sink.Save(data);
                }
            });
            return Disposable.Combine(sink, sub);
        }
    }
}
