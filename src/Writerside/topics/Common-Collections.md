# Collections and bit fields

## Bounded history

`CircularBuffer2<T>` is a fixed-capacity ring buffer. `PushBack` on a full buffer discards the oldest front item; `PushFront` discards the back item.

```csharp
using Asv.Common;

var history = new CircularBuffer2<int>(3);
history.PushBack(10);
history.PushBack(20);
history.PushBack(30);
history.PushBack(40);
Console.WriteLine(string.Join(", ", history.ToArray())); // 20, 30, 40
```

Indexes refer to logical order, not backing-array positions. The valid range is `0 <= index < Size`, including after wrapping. `Front`, `Back`, `PopFront`, and `PopBack` reject an empty buffer.

The constructor accepting an array uses that array as backing storage. Supply a positive logical capacity no larger than its length and keep the array alive for the buffer's lifetime. `ToArray` creates a copy; `ToArraySegments` exposes segments of the backing storage. `CopyTo` copies as many logical items as fit in the destination span. This class does not provide synchronization.

## Time-based collections

| Type | Purpose |
| --- | --- |
| `ConcurrentCircularTimeBuffer<T>` | Time-ordered items with age and count limits |
| `ConcurrentTimeDictionary<TKey, TValue>` | Keyed objects with a timestamp callback and explicit age cleanup |

`ConcurrentCircularTimeBuffer<T>.Push` inserts by timestamp, even if items arrive out of order. Its count limit is enforced on insertion. Call `ClearOld` or `ClearOldAndGetRemaining` to apply the age policy; expiration is not a background task.

`GetItemsWithTimeMoreThen(beginTime)` returns items strictly newer than the boundary, newest first. An item equal to the boundary is excluded. `GetAll` returns the stored items in ascending timestamp order.

`ConcurrentTimeDictionary` runs factory, update, and read callbacks while holding its locks. Keep callbacks short and avoid reentering the dictionary. Exceptions from `GetValues` and timestamp callbacks in `ClearOld` propagate to the caller while releasing their locks. Cleanup removes objects older than the configured age and timestamps in the future relative to its time service.

For both collections, a timestamp callback should consistently represent the ordering or age of an item. Dispose collections only after concurrent callers have stopped.

## Bit fields

`UintBitArray` stores up to 32 bits in a `uint`. Bit index zero is the least significant bit. The indexer accesses one bit; `GetBitU` and `SetBitU` access a range.

```csharp
using Asv.Common;

var flags = new UintBitArray(0, 32);
flags.SetBitU(5, 5, 0b1_1111U);
Console.WriteLine(flags.Value); // 992
Console.WriteLine(flags.GetBitU(5, 5)); // 31
Console.WriteLine(flags[5]); // True
```

Range access supports widths from zero through 32. A zero-width read returns zero and a zero-width write changes nothing. The start and width must fit within `Size`. Writes truncate the supplied value to the requested width and preserve bits outside the range. Negative indexes and single-bit indexes equal to `Size` are rejected.

`UintBitArray` is mutable and does not provide synchronization. Avoid modifying an instance while it is used as a dictionary key, because its equality and hash code depend on its value and size.
