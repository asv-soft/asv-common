using System;
using System.Threading;
using System.Threading.Tasks;
using Asv.Common;
using Microsoft.Extensions.Logging;
using R3;
using ZLogger;

namespace Asv.IO;

public abstract class ProtocolConnection : AsyncDisposableWithCancel, IProtocolConnection
{
    private readonly ILogger<ProtocolConnection> _logger;
    private readonly Subject<IProtocolMessage> _onTxMessage = new();
    private readonly Subject<IProtocolMessage> _onRxMessage = new();
    private readonly Subject<Exception> _onRxError = new();
    private readonly Subject<Exception> _onTxError = new();
    private ProtocolTags _tags = [];

    protected ProtocolConnection(
        string id,
        IProtocolContext context,
        IStatisticHandler? statistic = null
    )
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(context);
        _logger = context.LoggerFactory.CreateLogger<ProtocolConnection>();
        if (statistic == null)
        {
            var value = new Statistic();
            Statistic = value;
            StatisticHandler = value;
        }
        else
        {
            var value = new InheritedStatistic(statistic);
            Statistic = value;
            StatisticHandler = value;
        }
        Id = id;
        Context = context;
        foreach (var feature in Context.Features)
        {
            feature.Register(this);
        }
    }

    public string Id { get; }
    public IStatistic Statistic { get; }
    public Observable<IProtocolMessage> OnTxMessage => _onTxMessage;
    public Observable<Exception> OnTxError => _onTxError;
    public Observable<IProtocolMessage> OnRxMessage => _onRxMessage;
    public Observable<Exception> OnRxError => _onRxError;

    protected IStatisticHandler StatisticHandler { get; }
    protected IProtocolContext Context { get; }
    public abstract ValueTask Send(IProtocolMessage message, CancellationToken cancel = default);

    public string? PrintMessage(
        IProtocolMessage message,
        PacketFormatting formatting = PacketFormatting.Inline
    ) => Context.PrintMessage(message, formatting);

    protected async ValueTask<IProtocolMessage?> InternalFilterTxMessage(IProtocolMessage message)
    {
        foreach (var item in Context.Features)
        {
            var newMsg = await item.ProcessTx(message, this, DisposeCancel);
            if (newMsg == null)
            {
                return null;
            }

            message = newMsg;
        }
        return message;
    }

    [Obsolete]
    protected async void InternalPublishRxMessage(IProtocolMessage message)
    {
        try
        {
            foreach (var item in Context.Features)
            {
                var newMsg = await item.ProcessRx(message, this, DisposeCancel);
                if (newMsg == null)
                {
                    return;
                }

                message = newMsg;
            }
            _onRxMessage.OnNext(message);
        }
        catch (ProtocolConnectionException ex)
        {
            InternalPublishRxError(ex);
        }
        catch (Exception ex)
        {
            InternalPublishRxError(
                new ProtocolConnectionException(
                    this,
                    $"Error at publish rx message:{ex.Message}",
                    ex
                )
            );
        }
    }
    
    protected async Task InternalPublishRxMessageAsync(IProtocolMessage message)
    {
        try
        {
            foreach (var item in Context.Features)
            {
                var newMsg = await item.ProcessRx(message, this, DisposeCancel);
                if (newMsg == null)
                {
                    return;
                }

                message = newMsg;
            }
            _onRxMessage.OnNext(message);
        }
        catch (ProtocolConnectionException ex)
        {
            InternalPublishRxError(ex);
        }
        catch (Exception ex)
        {
            InternalPublishRxError(
                new ProtocolConnectionException(
                    this,
                    $"Error at publish rx message:{ex.Message}",
                    ex
                )
            );
        }
    }

    protected void InternalPublishRxError(Exception ex)
    {
        if (IsDisposed)
        {
            return;
        }

        _onRxError.OnNext(ex);
    }

    protected void InternalPublishTxError(ProtocolConnectionException ex)
    {
        if (IsDisposed)
        {
            return;
        }

        _onTxError.OnNext(ex);
    }

    protected void InternalPublishTxMessage(IProtocolMessage msg)
    {
        if (IsDisposed)
        {
            return;
        }

        _onTxMessage.OnNext(msg);
    }

    public ref ProtocolTags Tags => ref _tags;

    #region Dispose

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _logger.ZLogTrace($"{nameof(Dispose)} connection {Id}");
            foreach (var feature in Context.Features)
            {
                feature.Unregister(this);
            }
            _onTxError.Dispose();
            _onRxError.Dispose();
            _onTxMessage.Dispose();
            _onRxMessage.Dispose();
        }
        base.Dispose(disposing);
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        _logger.ZLogTrace($"{nameof(DisposeAsyncCore)} port {this}");
        foreach (var feature in Context.Features)
        {
            feature.Unregister(this);
        }
        await CastAndDispose(_onTxError);
        await CastAndDispose(_onRxError);
        await CastAndDispose(_onTxMessage);
        await CastAndDispose(_onRxMessage);
        await base.DisposeAsyncCore();

        return;

        static async ValueTask CastAndDispose(IDisposable resource)
        {
            if (resource is IAsyncDisposable resourceAsyncDisposable)
            {
                await resourceAsyncDisposable.DisposeAsync();
            }
            else
            {
                resource.Dispose();
            }
        }
    }

    #endregion
}
