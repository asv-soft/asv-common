using System;
using System.Threading;
using System.Threading.Tasks;
using Asv.Common;
using Microsoft.Extensions.Logging;
using R3;
using ZLogger;

namespace Asv.IO;

public interface IMicroserviceServer: IDisposable, IAsyncDisposable
{
    string Id { get; }
    string TypeName { get; }
    bool IsInit { get; }
    Task Init(CancellationToken cancel = default);
}

public abstract class MicroserviceServer<TBaseMessage> : AsyncDisposableWithCancel, IMicroserviceServer
    where TBaseMessage : IProtocolMessage
{
    private readonly ILogger _loggerBase;
    private readonly Subject<TBaseMessage> _internalFilteredDeviceMessages = new();
    private readonly IDisposable _sub1;

    protected MicroserviceServer(IDeviceContext context, string id)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(id);
        Context = context;
        Id = id;
        _loggerBase = context.LoggerFactory.CreateLogger(id);
        _sub1 = context.Connection.RxFilterByType<TBaseMessage>().Where(FilterDeviceMessages)
            .Subscribe(_internalFilteredDeviceMessages.AsObserver());
    }

    protected abstract bool FilterDeviceMessages(TBaseMessage arg);
    
    protected IDeviceContext Context { get; set; }
    
    protected Observable<TBaseMessage> InternalFilteredDeviceMessages => _internalFilteredDeviceMessages;

    public string Id { get; }
    public abstract string TypeName { get; }
    public bool IsInit { get; private set; }
    public async Task Init(CancellationToken cancel = default)
    {
        try
        {
            if (IsInit) return;
            _loggerBase.ZLogTrace($"Init microservice {TypeName}[{Id}]");
            await InternalInit(cancel);
            IsInit = true;
        }
        catch (Exception ex)
        {
            _loggerBase.ZLogError(ex, $"Error on init microservice {TypeName}[{Id}]");
            throw;
        }
    }
    
    protected virtual Task InternalInit(CancellationToken cancel)
    {
        return Task.CompletedTask;
    }
    
    
    #region Dispose

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _internalFilteredDeviceMessages.Dispose();
            _sub1.Dispose();
        }

        base.Dispose(disposing);
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        await CastAndDispose(_internalFilteredDeviceMessages);
        await CastAndDispose(_sub1);

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

