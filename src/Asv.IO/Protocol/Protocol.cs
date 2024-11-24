using System;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Asv.Common;
using Microsoft.Extensions.Logging;
using R3;
using ZLogger;

namespace Asv.IO;

public class Protocol: AsyncDisposableWithCancel, IProtocol, IProtocolContext
{
    #region Static

    public static IProtocol Create(Action<IProtocolBuilder> configure)
    {
        var builder = new ProtocolBuilder();
        configure(builder);
        return builder.Build();
    }
    
    private static NameValueCollection ParseQueryString(string requestQueryString)
    {
        var rc = new NameValueCollection();
        var ar1 = requestQueryString.Split('&', '?','#',';');
        foreach (var row in ar1)
        {
            if (string.IsNullOrEmpty(row)) continue;
            var index = row.IndexOf('=');
            if (index < 0) continue;
            rc[Uri.UnescapeDataString(row[..index])] = Uri.UnescapeDataString(row[(index + 1)..]); // use Unescape only parts          
        }
        return rc;
    }

    public const string ProtocolQueryKey = "protocols";
    private const char ValuesDelimiter = ',';
    
    #endregion

    private readonly Channel<IProtocolMessage> _rxChannel;
    private readonly Channel<ProtocolException> _errorChannel;
    private readonly Statistic _statistic;
    private ImmutableArray<IProtocolPort> _ports = [];
    private readonly Subject<ImmutableArray<IProtocolPort>> _portsChanged = new();
    private readonly Subject<IProtocolMessage> _onTxMessage = new();
    private readonly Subject<IProtocolMessage> _onRxMessage = new();
    private readonly ILogger _logger;
    private readonly Task _readLoopTask;
    private readonly Task _readErrorLoopTask;

    internal Protocol(
        Channel<IProtocolMessage> rxChannel, 
        Channel<ProtocolException> errorChannel, 
        ImmutableArray<IProtocolFeature> features, 
        ImmutableDictionary<string,ParserFactoryDelegate> parserFactory, 
        ImmutableArray<ProtocolInfo> availableProtocols, 
        ImmutableDictionary<string,PortFactoryDelegate> portFactory, 
        ImmutableArray<PortTypeInfo> availablePortTypes,
        ImmutableArray<IProtocolMessageFormatter> formatters, 
        ILoggerFactory loggerFactory, 
        TimeProvider timeProvider, 
        IMeterFactory meterFactory)
    {
        _logger = loggerFactory.CreateLogger<Protocol>();
        _rxChannel = rxChannel;
        _errorChannel = errorChannel;
        Features = features;
        ParserFactory = parserFactory;
        AvailableProtocols = availableProtocols;
        PortFactory = portFactory;
        AvailablePortTypes = availablePortTypes;
        Formatters = formatters;
        LoggerFactory = loggerFactory;
        TimeProvider = timeProvider;
        MeterFactory = meterFactory;
        _statistic = new Statistic();
        _readLoopTask = Task.Factory.StartNew(PublishRxLoop, TaskCreationOptions.LongRunning, DisposeCancel);
        _readErrorLoopTask = Task.Factory.StartNew(PublishErrorLoop, TaskCreationOptions.LongRunning, DisposeCancel);
    }

    private async void PublishErrorLoop(object? obj)
    {
        try
        {
            while (IsDisposed == false)
            {
                try
                {
                    var exception = await _errorChannel.Reader.ReadAsync(DisposeCancel);
                    _onRxMessage.OnErrorResume(exception);
                }
                catch (Exception e)
                {
                    _logger.ZLogError(e, $"Error in '{nameof(PublishErrorLoop)}':{e.Message}");
                    _onRxMessage.OnErrorResume(e);
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogCritical(e, "Error in publish loop");
            Debug.Assert(false);
            Debugger.Break();
        }
    }

    private async void PublishRxLoop(object? state)
    {
        try
        {
            while (IsDisposed == false)
            {
                try
                {
                    var message = await _rxChannel.Reader.ReadAsync(DisposeCancel);
                    _onRxMessage.OnNext(message);
                }
                catch (ProtocolException e)
                {
                    _logger.ZLogError(e, $"Error in '{nameof(PublishRxLoop)}':{e.Message}");
                    await _errorChannel.Writer.WriteAsync(e);
                }
                catch (Exception e)
                {
                    _logger.ZLogError(e, $"Error in '{nameof(PublishRxLoop)}':{e.Message}");
                    await _errorChannel.Writer.WriteAsync(new ProtocolException($"Error in '{nameof(PublishRxLoop)}':{e.Message}",e));
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogCritical(e, "Error in publish loop");
            Debug.Assert(false);
            Debugger.Break();
        }
    }

    public IStatistic Statistic => _statistic;
    public ImmutableArray<PortTypeInfo> AvailablePortTypes { get; }
    public ILoggerFactory LoggerFactory { get; }
    public TimeProvider TimeProvider { get; }
    public IMeterFactory MeterFactory { get; }
    public ImmutableDictionary<string, ParserFactoryDelegate> ParserFactory { get; }
    public ImmutableArray<IProtocolFeature> Features { get; }
    public ImmutableArray<ProtocolInfo> AvailableProtocols { get; }
    public ImmutableDictionary<string, PortFactoryDelegate> PortFactory { get; }
    public ImmutableArray<IProtocolMessageFormatter> Formatters { get; }
    public ChannelWriter<IProtocolMessage> RxChannel => _rxChannel.Writer;
    public ChannelWriter<ProtocolException> ErrorChannel => _errorChannel.Writer;
    
    public IProtocolPort AddPort(Uri connectionString)
    {
        if (!PortFactory.TryGetValue(connectionString.Scheme, out var factory))
        {
            throw new InvalidOperationException($"Port type {connectionString.Scheme} not found");
        }
        var additionalArgs = ParseQueryString(connectionString.Fragment);
        var protoStr = additionalArgs[ProtocolQueryKey];

        ImmutableHashSet<string> protocols;
        if (protoStr != null)
        {
            protocols = protoStr.Split(ValuesDelimiter).ToImmutableHashSet();
        }
        else
        {
            protocols = AvailableProtocols.Select(x => x.Id).ToImmutableHashSet();
        }
        foreach (var protocol in protocols)
        {
            if (ParserFactory.ContainsKey(protocol) == false)
            {
                throw new InvalidOperationException($"Parser for protocol '{protocol}' not found");
            }
        }

        var selectedProtocol =  AvailableProtocols.Where(x => protocols.Contains(x.Id)).ToImmutableArray();
        
        var args = new PortArgs
        {
            UserInfo = connectionString.UserInfo,
            Host = connectionString.Host,
            Port = connectionString.Port,
            Path = connectionString.AbsolutePath,
            Query = ParseQueryString(connectionString.Query)
        };
        var port = factory(args, selectedProtocol , this, _statistic);

        ImmutableArray<IProtocolPort> before,after;
        do
        {
            before = _ports;
            after = before.Add(port);    
        }
        // check if the value is changed by another thread while we are removing the endpoint
        while (ImmutableInterlocked.InterlockedCompareExchange(ref _ports, after, before) != before);
        _portsChanged.OnNext(after);
        return port;
    }

    public void RemovePort(IProtocolPort port)
    {
        ImmutableArray<IProtocolPort> before,after;
        do
        {
            before = _ports;
            after = before.Remove(port);    
        }
        // check if the value is changed by another thread while we are removing the endpoint
        while (ImmutableInterlocked.InterlockedCompareExchange(ref _ports, after, before) != before);
        _portsChanged.OnNext(after);
    }

    public ImmutableArray<IProtocolPort> Ports => _ports;

    public Observable<ImmutableArray<IProtocolPort>> PortsChanged => _portsChanged;

    public Observable<IProtocolMessage> OnTxMessage => _onTxMessage;

    public Observable<IProtocolMessage> OnRxMessage => _onRxMessage;

    public async ValueTask Send(IProtocolMessage message, CancellationToken cancel = default)
    {
        foreach (var port in _ports)
        {
            await port.Send(message, cancel);
        }
        _onTxMessage.OnNext(message);
    }

    public string? PrintMessage(IProtocolMessage message, PacketFormatting formatting = PacketFormatting.Inline)
    {
        return Formatters
            .Where(x => x.CanPrint(message))
            .Select(x => x.Print(message, formatting))
            .FirstOrDefault();
    }

    #region Dispose

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ImmutableArray<IProtocolPort> before,after;
            do
            {
                before = _ports;
                after = ImmutableArray<IProtocolPort>.Empty;
            }
            // check if the value is changed by another thread while we are removing the endpoint
            while (ImmutableInterlocked.InterlockedCompareExchange(ref _ports, after, before) != before);
            _portsChanged.OnNext(after);
            foreach (var port in _ports)
            {
                port.Dispose();
            }
            
            _portsChanged.Dispose();
            _onTxMessage.Dispose();
            _onRxMessage.Dispose();
            LoggerFactory.Dispose();
            MeterFactory.Dispose();
        }

        base.Dispose(disposing);
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        ImmutableArray<IProtocolPort> before,after;
        do
        {
            before = _ports;
            after = ImmutableArray<IProtocolPort>.Empty;
        }
        // check if the value is changed by another thread while we are removing the endpoint
        while (ImmutableInterlocked.InterlockedCompareExchange(ref _ports, after, before) != before);
        _portsChanged.OnNext(after);
        foreach (var port in _ports)
        {
            await port.DisposeAsync();
        }
        
        await CastAndDispose(_portsChanged);
        await CastAndDispose(_onTxMessage);
        await CastAndDispose(_onRxMessage);
        await CastAndDispose(LoggerFactory);
        await CastAndDispose(MeterFactory);

        await base.DisposeAsyncCore();

        return;

        static async ValueTask CastAndDispose(IDisposable resource)
        {
            if (resource is IAsyncDisposable resourceAsyncDisposable)
                await resourceAsyncDisposable.DisposeAsync();
            else
                resource.Dispose();
        }
    }
    
    #endregion

}