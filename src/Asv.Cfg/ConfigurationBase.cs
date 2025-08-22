using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Asv.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using R3;
using ZLogger;

namespace Asv.Cfg;

public abstract class ConfigurationBase(ILogger? logger = null) : AsyncDisposableOnce, IConfiguration
{
    protected readonly ILogger Logger = logger ?? NullLogger.Instance;
    private readonly Subject<ConfigurationException> _onError = new();
    private readonly Subject<KeyValuePair<string, object?>> _onChanged = new();

    public IEnumerable<string> ReservedParts
    {
        get
        {
            try
            {
                return InternalSafeGetReservedParts();
            }
            catch (Exception e)
            {
                throw InternalPublishError(new ConfigurationException($"Error to get reserved parts:{e.Message}",e));
            }
        }
    }

    protected abstract IEnumerable<string> InternalSafeGetReservedParts();
    public IEnumerable<string> AvailableParts
    {
        get
        {
            try
            {
                return InternalSafeGetAvailableParts();
            }
            catch (Exception e)
            {
                throw InternalPublishError(new ConfigurationException($"Error to get available parts",e));
            }
            
        }
    }

    protected abstract IEnumerable<string> InternalSafeGetAvailableParts();

    public bool Exist(string key)
    {
        try
        {
            ConfigurationMixin.ValidateKey(key);
            return InternalSafeExist(key);
        }
        catch (Exception e)
        {
            throw InternalPublishError(new ConfigurationException($"Error to check exist '{key}' part:{e.Message}"));
        }
    }

    protected abstract bool InternalSafeExist(string key);

    public TPocoType Get<TPocoType>(string key, Lazy<TPocoType> defaultValue)
    {
        try
        {
            ConfigurationMixin.ValidateKey(key);
            if (typeof(TPocoType).IsAssignableTo(typeof(ICustomConfigurable)))
            {
                if (defaultValue.Value is ICustomConfigurable cfg)
                {
                    cfg.Load(key,this);
                    return defaultValue.Value;
                }
            }
            return InternalSafeGet(key,defaultValue);  
        }
        catch (Exception e)
        {
            throw InternalPublishError(new ConfigurationException($"Error to get exist '{key}' part",e));
        }
    }

    protected abstract TPocoType InternalSafeGet<TPocoType>(string key, Lazy<TPocoType> defaultValue);

    public void Set<TPocoType>(string key, TPocoType value)
    {
        try
        {
            ConfigurationMixin.ValidateKey(key);
            if (value is ICustomConfigurable cfg)
            {
                cfg.Save(key,this);
                return;
            }
            InternalSafeSave(key,value);
            Logger.ZLogTrace($"Set configuration key [{key}]");
            _onChanged.OnNext(new KeyValuePair<string, object?>(key,value));
        }
        catch (Exception e)
        {
            throw InternalPublishError(new ConfigurationException($"Error to set exist '{key}' part",e)); 
        }
    }

    protected abstract void InternalSafeSave<TPocoType>(string key, TPocoType value);

    public void Remove(string key)
    {
        try
        {
            ConfigurationMixin.ValidateKey(key);
            InternalSafeRemove(key);
            Logger.ZLogTrace($"Remove configuration key [{key}]");
            _onChanged.OnNext(new KeyValuePair<string, object?>(key,null));
        }
        catch (Exception e)
        {
            throw InternalPublishError(new ConfigurationException($"Error to remove exist '{key}' part",e));
        }
    }

    protected abstract void InternalSafeRemove(string key);

    protected ConfigurationException InternalPublishError(ConfigurationException e)
    {
        Logger.ZLogError(e,$"{this} error: {e.Message}");
        _onError.OnNext(e);
        return e;
    }
    
    public Observable<ConfigurationException> OnError => _onError;
    public Observable<KeyValuePair<string, object?>> OnChanged => _onChanged;

    #region Dispose

    protected override void Dispose(bool disposing)
    {
        Logger.ZLogTrace($"Dispose {GetType().Name}");
        if (disposing)
        {
            _onError.Dispose();
            _onChanged.Dispose();
        }

        base.Dispose(disposing);
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        _onError.Dispose();
        _onChanged.Dispose();
        await base.DisposeAsyncCore();
    }

    #endregion
}