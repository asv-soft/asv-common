using System;
using System.Buffers;

namespace Asv.Common;

public interface IChange
{
    void Serialize(IBufferWriter<byte> writer);
    void Deserialize(ReadOnlySequence<byte> data);
}

public interface IUndoController : IDisposable
{
    bool MuteChanges { get; set; }
    IDisposable Register(IUndoHandler handler);
    IUndoHandler Find(string registrationId);
}
