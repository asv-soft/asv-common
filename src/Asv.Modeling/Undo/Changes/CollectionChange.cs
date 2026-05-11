using System.Buffers;
using System.Collections.Specialized;
using MemoryPack;
using ObservableCollections;

namespace Asv.Modeling;

[MemoryPackable]
public partial struct CollectionChange<T> : IChange<T>
{
    public ChangeOperation Operation { get; set; }
    public int OldStartingIndex { get; set; }
    public int NewStartingIndex { get; set; }
    public T[] OldItems { get; set; }
    public T[] NewItems { get; set; }

    [MemoryPackIgnore]
    public int OldIndex
    {
        get => OldStartingIndex;
        set => OldStartingIndex = value;
    }

    [MemoryPackIgnore]
    public int NewIndex
    {
        get => NewStartingIndex;
        set => NewStartingIndex = value;
    }

    [MemoryPackIgnore]
    public T OldValue
    {
        get => OldItems is { Length: > 0 } ? OldItems[0] : default!;
        set => OldItems = [value];
    }

    [MemoryPackIgnore]
    public T NewValue
    {
        get => NewItems is { Length: > 0 } ? NewItems[0] : default!;
        set => NewItems = [value];
    }

    [MemoryPackOnDeserializing]
    private void OnDeserializing()
    {
        OldItems = [];
        NewItems = [];
    }

    public static CollectionChange<T> From(
        NotifyCollectionChangedEventArgs<T> args
    )
    {
        return args.Action switch
        {
            NotifyCollectionChangedAction.Add => new CollectionChange<T>
            {
                Operation = ChangeOperation.Create,
                OldStartingIndex = -1,
                NewStartingIndex = args.NewStartingIndex,
                OldItems = [],
                NewItems = ToArray(args.IsSingleItem, args.NewItem, args.NewItems),
            },
            NotifyCollectionChangedAction.Remove => new CollectionChange<T>
            {
                Operation = ChangeOperation.Delete,
                OldStartingIndex = args.OldStartingIndex,
                NewStartingIndex = -1,
                OldItems = ToArray(args.IsSingleItem, args.OldItem, args.OldItems),
                NewItems = [],
            },
            NotifyCollectionChangedAction.Replace => new CollectionChange<T>
            {
                Operation = ChangeOperation.Update,
                OldStartingIndex = args.OldStartingIndex,
                NewStartingIndex = args.NewStartingIndex,
                OldItems = ToArray(args.IsSingleItem, args.OldItem, args.OldItems),
                NewItems = ToArray(args.IsSingleItem, args.NewItem, args.NewItems),
            },
            _ => throw new NotSupportedException(
                $"Collection change action '{args.Action}' is not supported by undo history"
            ),
        };
    }

    private static T[] ToArray(bool isSingleItem, T item, ReadOnlySpan<T> items)
    {
        return isSingleItem ? [item] : items.ToArray();
    }

    public void Serialize(IBufferWriter<byte> writer)
    {
        MemoryPackSerializer.Serialize(writer, this);
    }

    public void Deserialize(ReadOnlySequence<byte> data)
    {
        MemoryPackSerializer.Deserialize(in data, ref this);
    }
}
