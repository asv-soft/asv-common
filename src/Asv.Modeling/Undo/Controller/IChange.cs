using System.Buffers;

namespace Asv.Modeling;

public interface IChange
{
    void Serialize(IBufferWriter<byte> writer);
    void Deserialize(ReadOnlySequence<byte> data);
}

public enum ChangeOperation : byte
{
    Update = 0,
    Create = 1,
    Read = 2,
    Delete = 3,
}

public interface IChange<T> : IChange
{
    ChangeOperation Operation { get; set; }
    T OldValue { get; set; }
    T NewValue { get; set; }
}