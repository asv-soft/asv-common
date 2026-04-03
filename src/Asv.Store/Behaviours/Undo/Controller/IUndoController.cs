using System;
using System.Buffers;
using R3;

namespace Asv.Common;

public interface IChange
{
    void Serialize(IBufferWriter<byte> writer);
    void Deserialize(ReadOnlySequence<byte> data);
}

public interface IUndoController : IDisposable
{
    bool MuteUndoChanges { get; set; }
    IDisposable Register(IUndoHandler handler);
    IUndoHandler Find(string registrationId);
}

public static class UndoControllerMixin
{
    public static IDisposable Register<T>(this IUndoController controller, string name, ReactiveProperty<T> prop)
    {
        return controller.Register(new ReactivePropertyChangeHandler<T>(name, prop));
    }
}