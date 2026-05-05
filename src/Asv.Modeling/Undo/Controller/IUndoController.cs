using System.Buffers;

namespace Asv.Modeling;

public interface IChange
{
    void Serialize(IBufferWriter<byte> writer);
    void Deserialize(ReadOnlySequence<byte> data);
}

public interface IUndoController : IDisposable
{
    bool SuppressChanges { get; set; }
    void Register(IUndoHandler handler);
    void Unregister(IUndoHandler handler);
    IUndoHandler Find(string registrationId);
}
