using System.Buffers;
using System.Collections.Specialized;
using System.Runtime.Serialization;
using MessagePack;
using ObservableCollections;

namespace Asv.Modeling;

[DataContract]
public struct CollectionUndoChange<T> : IUndoChange<T>
{
    [DataMember(Order = 0)]
    public ChangeOperation Operation { get; set; }

    [DataMember(Order = 1)]
    public int OldStartingIndex { get; set; }

    [DataMember(Order = 2)]
    public int NewStartingIndex { get; set; }

    [DataMember(Order = 3)]
    public T[] OldItems { get; set; }

    [DataMember(Order = 4)]
    public T[] NewItems { get; set; }

    [IgnoreDataMember]
    public int OldIndex
    {
        get => OldStartingIndex;
        set => OldStartingIndex = value;
    }

    [IgnoreDataMember]
    public int NewIndex
    {
        get => NewStartingIndex;
        set => NewStartingIndex = value;
    }

    [IgnoreDataMember]
    public T OldValue
    {
        get => OldItems is { Length: > 0 } ? OldItems[0] : default!;
        set => OldItems = [value];
    }

    [IgnoreDataMember]
    public T NewValue
    {
        get => NewItems is { Length: > 0 } ? NewItems[0] : default!;
        set => NewItems = [value];
    }

    public static CollectionUndoChange<T> From(NotifyCollectionChangedEventArgs<T> args)
    {
        return args.Action switch
        {
            NotifyCollectionChangedAction.Add => new CollectionUndoChange<T>
            {
                Operation = ChangeOperation.Create,
                OldStartingIndex = -1,
                NewStartingIndex = args.NewStartingIndex,
                OldItems = [],
                NewItems = ToArray(args.IsSingleItem, args.NewItem, args.NewItems),
            },
            NotifyCollectionChangedAction.Remove => new CollectionUndoChange<T>
            {
                Operation = ChangeOperation.Delete,
                OldStartingIndex = args.OldStartingIndex,
                NewStartingIndex = -1,
                OldItems = ToArray(args.IsSingleItem, args.OldItem, args.OldItems),
                NewItems = [],
            },
            NotifyCollectionChangedAction.Replace => new CollectionUndoChange<T>
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
        MessagePackSerializer.Serialize(writer, this);
    }

    public void Deserialize(ReadOnlySequence<byte> data)
    {
        this = MessagePackSerializer.Deserialize<CollectionUndoChange<T>>(data);
        OldItems ??= [];
        NewItems ??= [];
    }
}
