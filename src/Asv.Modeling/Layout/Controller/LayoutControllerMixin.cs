using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Asv.Modeling;

public static partial class LayoutControllerMixin
{
    extension(ILayoutController controller)
    {
        public ILayoutRegistration Create<TData>(
            string layoutId,
            AsyncLoadLayoutCallback<TData> load,
            AsyncSaveLayoutCallback<TData> save
        )
            where TData : IJsonLayoutData<TData>, new()
        {
            return controller.Create(layoutId, load, save, static () => new TData());
        }

        public ILayoutRegistration Create<TData>(
            string layoutId,
            LoadLayoutCallback<TData> load,
            SaveLayoutCallback<TData> save,
            Func<TData> factory
        )
            where TData : IJsonLayoutData<TData>
        {
            return controller.Create(
                layoutId,
                (data, _) =>
                {
                    load(data);
                    return ValueTask.CompletedTask;
                },
                (data, _) =>
                {
                    save(data);
                    return ValueTask.CompletedTask;
                },
                factory
            );
        }

        public ILayoutRegistration Create<TData>(
            string layoutId,
            LoadLayoutCallback<TData> load,
            SaveLayoutCallback<TData> save
        )
            where TData : IJsonLayoutData<TData>, new()
        {
            return controller.Create(layoutId, load, save, static () => new TData());
        }

        public ILayoutRegistration Create<TData>(string layoutId, TData data)
            where TData : IMutableLayoutData<TData>, new()
        {
            ArgumentNullException.ThrowIfNull(data);
            return controller.Create(
                layoutId,
                data.CopyFrom,
                saved => saved.CopyFrom(data),
                static () => new TData()
            );
        }

        public ILayoutRegistration Create(string layoutId, Func<bool> get, Action<bool> set)
        {
            ArgumentNullException.ThrowIfNull(get);
            ArgumentNullException.ThrowIfNull(set);

            return controller.Create<BoolLayoutData>(
                layoutId,
                data => set(data.Value),
                data => data.Value = get()
            );
        }

        public ILayoutRegistration Create<TValue>(
            string layoutId,
            Func<TValue> get,
            Action<TValue> set
        )
        {
            ArgumentNullException.ThrowIfNull(get);
            ArgumentNullException.ThrowIfNull(set);

            if (typeof(TValue) == typeof(bool))
            {
                return controller.Create(
                    layoutId,
                    () =>
                    {
                        var value = get();
                        return value is bool boolValue
                            ? boolValue
                            : throw new InvalidOperationException(
                                $"Layout value type '{typeof(TValue).FullName}' is not Boolean."
                            );
                    },
                    value =>
                    {
                        object boxedValue = value;
                        set((TValue)boxedValue);
                    }
                );
            }

            throw new NotSupportedException(
                $"Layout value type '{typeof(TValue).FullName}' does not have source-generated JSON metadata."
            );
        }
    }

    private sealed class BoolLayoutData : IJsonLayoutData<BoolLayoutData>
    {
        public static JsonTypeInfo<BoolLayoutData> JsonTypeInfo =>
            LayoutControllerMixinJsonContext.Default.BoolLayoutData;

        public bool Value { get; set; }
    }

    [JsonSerializable(typeof(BoolLayoutData))]
    private sealed partial class LayoutControllerMixinJsonContext : JsonSerializerContext;
}
