using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Asv.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using R3;
using ZLogger;

namespace Asv.Cfg;

/// <summary>
/// Provides a base implementation for configuration stores.
/// </summary>
public abstract class ConfigurationBase(ILogger? logger = null)
    : AsyncDisposableOnce,
        IConfiguration
{
    /// <summary>
    /// Gets the logger used by this configuration instance.
    /// </summary>
    protected ILogger Logger { get; } = logger ?? NullLogger.Instance;
    private readonly Subject<ConfigurationException> _onError = new();
    private readonly Subject<KeyValuePair<string, object?>> _onChanged = new();

    /// <inheritdoc />
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
                throw InternalPublishError(
                    new ConfigurationException($"Error to get reserved parts:{e.Message}", e)
                );
            }
        }
    }

    /// <summary>
    /// Gets keys reserved by the concrete configuration store.
    /// </summary>
    /// <returns>The reserved configuration keys.</returns>
    protected abstract IEnumerable<string> InternalSafeGetReservedParts();

    /// <inheritdoc />
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
                throw InternalPublishError(
                    new ConfigurationException($"Error to get available parts", e)
                );
            }
        }
    }

    /// <summary>
    /// Gets keys available in the concrete configuration store.
    /// </summary>
    /// <returns>The available configuration keys.</returns>
    protected abstract IEnumerable<string> InternalSafeGetAvailableParts();

    /// <inheritdoc />
    public bool Exist(string key)
    {
        try
        {
            ConfigurationMixin.ValidateKey(key);
            return InternalSafeExist(key);
        }
        catch (Exception e)
        {
            throw InternalPublishError(
                new ConfigurationException($"Error to check exist '{key}' part:{e.Message}")
            );
        }
    }

    /// <summary>
    /// Determines whether a key exists in the concrete configuration store.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <returns><see langword="true"/> if the key exists; otherwise, <see langword="false"/>.</returns>
    protected abstract bool InternalSafeExist(string key);

    /// <inheritdoc />
    public TPocoType Get<TPocoType>(string key, Lazy<TPocoType> defaultValue)
    {
        try
        {
            ConfigurationMixin.ValidateKey(key);
            if (typeof(TPocoType).IsAssignableTo(typeof(ICustomConfigurable)))
            {
                if (defaultValue.Value is ICustomConfigurable cfg)
                {
                    cfg.Load(key, this);
                    return defaultValue.Value;
                }
            }
            return InternalSafeGet(key, defaultValue);
        }
        catch (Exception e)
        {
            throw InternalPublishError(
                new ConfigurationException($"Error to get exist '{key}' part", e)
            );
        }
    }

    /// <summary>
    /// Gets a value from the concrete configuration store.
    /// </summary>
    /// <typeparam name="TPocoType">The configuration value type.</typeparam>
    /// <param name="key">The configuration key.</param>
    /// <param name="defaultValue">The default value factory used when the key is missing.</param>
    /// <returns>The loaded configuration value.</returns>
    protected abstract TPocoType InternalSafeGet<TPocoType>(
        string key,
        Lazy<TPocoType> defaultValue
    );

    /// <inheritdoc />
    public void Set<TPocoType>(string key, TPocoType value)
    {
        try
        {
            ConfigurationMixin.ValidateKey(key);
            if (value is ICustomConfigurable cfg)
            {
                cfg.Save(key, this);
                return;
            }
            InternalSafeSave(key, value);
            Logger.ZLogTrace($"Set configuration key [{key}]");
            _onChanged.OnNext(new KeyValuePair<string, object?>(key, value));
        }
        catch (Exception e)
        {
            throw InternalPublishError(
                new ConfigurationException($"Error to set exist '{key}' part", e)
            );
        }
    }

    /// <summary>
    /// Saves a value into the concrete configuration store.
    /// </summary>
    /// <typeparam name="TPocoType">The configuration value type.</typeparam>
    /// <param name="key">The configuration key.</param>
    /// <param name="value">The value to save.</param>
    protected abstract void InternalSafeSave<TPocoType>(string key, TPocoType value);

    /// <inheritdoc />
    public void Remove(string key)
    {
        try
        {
            ConfigurationMixin.ValidateKey(key);
            InternalSafeRemove(key);
            Logger.ZLogTrace($"Remove configuration key [{key}]");
            _onChanged.OnNext(new KeyValuePair<string, object?>(key, null));
        }
        catch (Exception e)
        {
            throw InternalPublishError(
                new ConfigurationException($"Error to remove exist '{key}' part", e)
            );
        }
    }

    /// <summary>
    /// Removes a value from the concrete configuration store.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    protected abstract void InternalSafeRemove(string key);

    /// <summary>
    /// Publishes a configuration error and returns it for throwing by the caller.
    /// </summary>
    /// <param name="e">The configuration error.</param>
    /// <returns>The same configuration error.</returns>
    protected ConfigurationException InternalPublishError(ConfigurationException e)
    {
        Logger.ZLogError(e, $"{this} error: {e.Message}");
        _onError.OnNext(e);
        return e;
    }

    /// <inheritdoc />
    public Observable<ConfigurationException> OnError => _onError;

    /// <inheritdoc />
    public Observable<KeyValuePair<string, object?>> OnChanged => _onChanged;

    #region Dispose

    /// <inheritdoc />
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

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore()
    {
        _onError.Dispose();
        _onChanged.Dispose();
        await base.DisposeAsyncCore();
    }

    #endregion
}
