using System.Buffers;

namespace Asv.Modeling;

public static class LayoutControllerMixin
{
    extension(ILayoutController controller)
    {
        public ILayoutRegistration Create<TData>(
            string layoutId,
            AsyncLoadLayoutCallback<TData> load,
            AsyncSaveLayoutCallback<TData> save
        )
            where TData : ILayoutData, new()
        {
            return controller.Create(layoutId, load, save, static () => new TData());
        }

        public ILayoutRegistration Create<TData>(
            string layoutId,
            LoadLayoutCallback<TData> load,
            SaveLayoutCallback<TData> save,
            Func<TData> factory
        )
            where TData : ILayoutData
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
            where TData : ILayoutData, new()
        {
            return controller.Create(layoutId, load, save, static () => new TData());
        }

        public ILayoutRegistration Create<TData>(string layoutId, TData data)
            where TData : ILayoutData, new()
        {
            ArgumentNullException.ThrowIfNull(data);
            return controller.Create(
                layoutId,
                loaded => Copy(loaded, data),
                saved => Copy(data, saved),
                static () => new TData()
            );
        }
    }

    private static void Copy(ILayoutData source, ILayoutData destination)
    {
        var writer = new ArrayBufferWriter<byte>();
        source.Serialize(writer);
        destination.Deserialize(new ReadOnlySequence<byte>(writer.WrittenMemory));
    }
}
