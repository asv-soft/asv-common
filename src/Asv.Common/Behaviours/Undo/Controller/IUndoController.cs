using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;
using R3;

namespace Asv.Common;

public delegate ValueTask DeserializeDelegate(SequenceReader<byte> reader, CancellationToken cancel);
public delegate ValueTask SerializeDelegate(object data, IBufferWriter<byte> writer, CancellationToken cancel);
public delegate ValueTask UndoDelegate(object data, CancellationToken cancel);
public delegate ValueTask RedoDelegate(object data, CancellationToken cancel);

public class UndoHandler
{
    public string Id { get; }

    public ValueTask Undo(object data)
    {
            
    }
        
    public ValueTask Redo(object data)
    {
            
    }

    public void Serialize(object data, IBufferWriter<byte> writer, CancellationToken cancel)
    {
            
    }
}

public interface IUndoController<TBase, TId>
{
    IDisposable Register<TChange>(
        string updateIdentifier, 
        Observable<TChange> changesPipe, 
        Action<TChange, IBufferWriter<byte>> serialize, 
        Action<TChange, Span<byte>> deserialize,
        Func<TChange, CancellationToken, ValueTask> undo, 
        Func<TChange, CancellationToken, ValueTask> redo);
}

public class UndoController<TBase, TId>(TBase owner)
    : IUndoController<TBase, TId>
    where TBase : ISupportRoutedEvents<TBase>
{
    public IDisposable Register<TChange>(string updateIdentifier, Observable<TChange> generator, Action<TChange> undo, Action<TChange> redo)
    {
        var sub1 = generator.SubscribeAwait((change, c) => owner.Rise(new UndoMutationEvent<TBase>(owner, change), c));
        
        return Disposable.Combine(
            ,
            owner.Subscribe<TBase, UndoEvent<TBase>>(Undo));
    }

    public IDisposable Register<TChange>(string updateIdentifier, Observable<TChange> changesPipe, Action<TChange, IBufferWriter<byte>> serialize, Action<TChange, Span<byte>> deserialize,
        Func<TChange, CancellationToken, ValueTask> undo, Func<TChange, CancellationToken, ValueTask> redo)
    {
        throw new NotImplementedException();
    }
}