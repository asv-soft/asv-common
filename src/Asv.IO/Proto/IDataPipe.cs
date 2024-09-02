using System;
using System.Threading;
using System.Threading.Tasks;
using Asv.Common;

namespace Asv.IO.Protocol;

public interface IDataPipe
{
    Task Read(Memory<byte> buffer, CancellationToken cancel = default);
    Task Write(ReadOnlyMemory<byte> buffer, CancellationToken cancel = default);
}

public class ProtocolId
{
    
}

public class MessageId
{
    public ProtocolId Protocol { get; }
}

public interface IProtocolMessage
{
    MessageId Id { get; }
}

public interface IProtocolDecoder
{
    ProtocolId Id { get; }
    IObservable<IProtocolMessage> OnMessage { get; }
    IObservable<ProtocolException> OnError { get; }
    bool Read(byte data);
    void Reset();
    bool TyrCreateMessage(MessageId id, out IProtocolMessage? message);
}

public interface IProtocolConnection
{
    IObservable<IProtocolMessage> OnMessage { get; }
    IObservable<ProtocolException> OnError { get; }
}

public abstract class ProtocolConnection : DisposableOnceWithCancel, IProtocolConnection
{
    public IObservable<IProtocolMessage> OnMessage { get; }
    public IObservable<ProtocolException> OnError { get; }
}

public abstract class SimpleProtocolConnection : ProtocolConnection
{
    protected abstract int BytesAvailable { get; }
    protected abstract ValueTask<int> Read(Memory<byte> buffer, CancellationToken cancel = default);
    protected abstract ValueTask Write(ReadOnlyMemory<byte> buffer, CancellationToken cancel = default);
}

public abstract class CombinedProtocolConnection : ProtocolConnection
{
    protected abstract void AddConnection(IProtocolConnection connection);
    protected abstract void RemoveConnection(IProtocolConnection connection);
}

public class ProtocolException : Exception
{
    public ProtocolException()
    {
    }

    public ProtocolException(string message) : base(message)
    {
    }

    public ProtocolException(string message, Exception inner) : base(message, inner)
    {
    }
}