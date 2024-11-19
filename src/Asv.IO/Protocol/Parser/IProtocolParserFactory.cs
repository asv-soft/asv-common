using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ObservableCollections;

namespace Asv.IO;

public class ProtocolParserInfo(string protocolId,string name, string description)
{
    public string ProtocolId { get; } = protocolId;
    public string Name { get; } = name;
    
    public string Description { get; } = description;

    public override string ToString()
    {
        return $"{Name}[{ProtocolId}]";
    }
}

public interface IProtocolParserFactory:IDisposable, IAsyncDisposable
{
    IReadOnlyObservableList<ProtocolParserInfo> Available { get; }
    IProtocolParser Create(string protocolId);
    void Register<TMessage, TMessageId>(ProtocolParserInfo info,
        ProtocolParserFactoryDelegate<TMessage, TMessageId> parserFactory, IEnumerable<Func<TMessage>> messageFactory)
        where TMessage : IProtocolMessage<TMessageId>
        where TMessageId : notnull;
}

public delegate IProtocolParser ProtocolParserFactoryDelegate<TMessage,TMessageId>(ImmutableDictionary<TMessageId,TMessage> factory, IProtocolCore core)
    where TMessage: IProtocolMessage<TMessageId> 
    where TMessageId : notnull;

public sealed class ProtocolParserFactory : IProtocolParserFactory
{
    private readonly IProtocolCore _core;
    private readonly ObservableList<ProtocolParserInfo> _available = new();
    private readonly SortedDictionary<string, Func<IProtocolParser>> _parsers = new();
    private readonly Dictionary<string, object?> _cache = new();
    private readonly ReaderWriterLockSlim _lock = new();
    private int _isDisposed;

    public ProtocolParserFactory(IProtocolCore core)
    {
        ArgumentNullException.ThrowIfNull(core);
        _core = core;
    }

    public IReadOnlyObservableList<ProtocolParserInfo> Available => _available;

    public IProtocolParser Create(string protocolId)
    {
        ArgumentNullException.ThrowIfNull(protocolId);
        _lock.EnterReadLock();
        try
        {
            if (_parsers.TryGetValue(protocolId, out var parserFactory))
            {
                return parserFactory();
            }
        }
        finally
        {
            _lock.ExitReadLock();
        }

        throw new InvalidOperationException($"Parser for protocol '{protocolId}' not found");
    }

    public void Register<TMessage, TMessageId>(ProtocolParserInfo info, ProtocolParserFactoryDelegate<TMessage,TMessageId> parserFactory, IEnumerable<Func<TMessage>> messageFactory)
        where TMessage: IProtocolMessage<TMessageId> 
        where TMessageId : notnull
    {
        ArgumentNullException.ThrowIfNull(info);
        ArgumentNullException.ThrowIfNull(parserFactory);
        ArgumentNullException.ThrowIfNull(messageFactory);
        _lock.EnterWriteLock();
        try
        {
            _parsers.Add(info.ProtocolId, LazyCreateParser);
            _available.Add(info);
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        return;

        IProtocolParser LazyCreateParser()
        {
            ImmutableDictionary<TMessageId, TMessage> factory;
            lock (_cache)
            {
                if (_cache.TryGetValue(info.ProtocolId, out var value))
                {
                    Debug.Assert(value != null, nameof(value) + " != null");
                    factory = (ImmutableDictionary<TMessageId, TMessage>)value;
                }
                else
                {
                    var builder = ImmutableDictionary.CreateBuilder<TMessageId, TMessage>();
                    // ReSharper disable once PossibleMultipleEnumeration
                    // we are sure that the messageFactory enumerate only once because we check cache
                    foreach (var message in messageFactory)
                    {
                        var msg = message();
                        builder.Add(msg.Id, msg);
                    }
                    _cache.Add(info.ProtocolId, factory = builder.ToImmutable());
                }
            }
            return parserFactory(factory, _core);
        }
        
        
    }

    public void Dispose()
    {
        // Make sure we're the first call to Dispose
        if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) == 1)
        {
            return;
        }
        _lock.EnterWriteLock();
        try
        {
            _parsers.Clear();
            _cache.Clear();
            _available.Clear();
        }
        finally
        {
            _lock.ExitWriteLock();
        }
        _lock.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        // Make sure we're the first call to DisposeAsync
        if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) == 1)
        {
            return ValueTask.CompletedTask;
        }
        _lock.EnterWriteLock();
        try
        {
            _parsers.Clear();
            _available.Clear();
        }
        finally
        {
            _lock.ExitWriteLock();
        }
        lock (_cache)
        {
            _cache.Clear();
        }
        _lock.Dispose();
        return ValueTask.CompletedTask;
    }
}