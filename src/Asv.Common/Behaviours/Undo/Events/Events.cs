using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

namespace Asv.Common;

public delegate ValueTask UndoResotreDelegate(object data, IBufferWriter<byte> writer, CancellationToken cancel);

public class UndoMutationEvent<TBase>(TBase sender, object change, UndoResotreDelegate serializer) 
    : AsyncRoutedEvent<TBase>(sender, RoutingStrategy.Bubble) 
    where TBase : ISupportRoutedEvents<TBase>
{
    public ValueTask Serialize(IBufferWriter<byte> writer, CancellationToken cancel)
    {
        return serializer(change, writer, cancel);
    }
}
    
public class UndoEvent<TBase>(TBase sender, SequenceReader<byte> data) 
    : AsyncRoutedEvent<TBase>(sender, RoutingStrategy.Direct)
    where TBase : ISupportRoutedEvents<TBase>
{
    
}

public class RedoEvent<TBase>(TBase sender, SequenceReader<byte> data) 
    : AsyncRoutedEvent<TBase>(sender, RoutingStrategy.Direct)
    where TBase : ISupportRoutedEvents<TBase>
{
    
}