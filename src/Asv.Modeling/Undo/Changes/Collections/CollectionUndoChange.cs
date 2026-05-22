using System.Buffers;
using System.Collections.Specialized;
using System.Runtime.Serialization;
using MessagePack;
using ObservableCollections;

namespace Asv.Modeling;

/// <summary>
/// Represents a serializable undo change for an observable collection operation.
/// </summary>
/// <typeparam name="T">The collection item type.</typeparam>
[DataContract]
public struct CollectionUndoChange<T> : IValueUndoChange<T>
{
    /// <inheritdoc />
    [DataMember(Order = 0)]
    public ChangeOperation Operation { get; set; }

    /// <summary>
    /// Gets or sets the starting index of old items.
    /// </summary>
    [DataMember(Order = 1)]
    public int OldStartingIndex { get; set; }

    /// <summary>
    /// Gets or sets the starting index of new items.
    /// </summary>
    [DataMember(Order = 2)]
    public int NewStartingIndex { get; set; }

    /// <summary>
    /// Gets or sets the items that existed before the collection change.
    /// </summary>
    [DataMember(Order = 3)]
    public T[] OldItems { get; set; }

    /// <summary>
    /// Gets or sets the items that exist after the collection change.
    /// </summary>
    [DataMember(Order = 4)]
    public T[] NewItems { get; set; }

    /// <summary>
    /// Gets or sets the old item index for single-item changes.
    /// </summary>
    [IgnoreDataMember]
    public int OldIndex
    {
        get => OldStartingIndex;
        set => OldStartingIndex = value;
    }

    /// <summary>
    /// Gets or sets the new item index for single-item changes.
    /// </summary>
    [IgnoreDataMember]
    public int NewIndex
    {
        get => NewStartingIndex;
        set => NewStartingIndex = value;
    }

    /// <inheritdoc />
    [IgnoreDataMember]
    public T OldValue
    {
        get => OldItems is { Length: > 0 } ? OldItems[0] : default!;
        set => OldItems = [value];
    }

    /// <inheritdoc />
    [IgnoreDataMember]
    public T NewValue
    {
        get => NewItems is { Length: > 0 } ? NewItems[0] : default!;
        set => NewItems = [value];
    }

    /// <summary>
    /// Creates a collection undo change from observable collection event arguments.
    /// </summary>
    /// <param name="args">The observable collection event arguments.</param>
    /// <returns>The collection undo change.</returns>
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

    /// <inheritdoc />
    public void Serialize(IBufferWriter<byte> writer)
    {
        MessagePackSerializer.Serialize(writer, this);
    }

    /// <inheritdoc />
    public void Deserialize(ReadOnlySequence<byte> data)
    {
        this = MessagePackSerializer.Deserialize<CollectionUndoChange<T>>(data);
        OldItems ??= [];
        NewItems ??= [];
    }
}
