using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using R3;

namespace Asv.Cfg;

/// <summary>
/// Provides a base implementation for JSON-backed configuration stores.
/// </summary>
public abstract class JsonConfigurationBase : ConfigurationBase
{
    private readonly ConcurrentDictionary<string, JToken> _values;
    private readonly Subject<Unit> _onNeedToSave = new();
    private readonly JsonSerializer _serializer;
    private readonly Lock _sync = new();
    private readonly bool _sortKeysInFile;
    private readonly IDisposable _saveSubscribe;
    private readonly bool _deferredFlush;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonConfigurationBase"/> class.
    /// </summary>
    /// <param name="loadCallback">The callback that opens a stream for loading configuration data.</param>
    /// <param name="flushToFileDelayMs">The optional delay used to defer saves.</param>
    /// <param name="sortKeysInFile">A value indicating whether keys are sorted when saved.</param>
    /// <param name="timeProvider">The time provider used for deferred saves.</param>
    protected JsonConfigurationBase(
        Func<Stream> loadCallback,
        TimeSpan? flushToFileDelayMs = null,
        bool sortKeysInFile = false,
        TimeProvider? timeProvider = null
    )
    {
        _sortKeysInFile = sortKeysInFile;
        _serializer = JsonHelper.CreateDefaultJsonSerializer();
        _serializer.Converters.Add(new StringEnumConverter());

        timeProvider ??= TimeProvider.System;
        _deferredFlush = flushToFileDelayMs != null;
        _saveSubscribe =
            flushToFileDelayMs == null
                ? _onNeedToSave.Subscribe(InternalSaveChanges)
                : _onNeedToSave
                    .ThrottleLast(flushToFileDelayMs.Value, timeProvider)
                    .Subscribe(InternalSaveChanges, x => InternalSaveChanges(Unit.Default));

        using var stream = loadCallback();
        using var reader = new StreamReader(stream);
        _values = new ConcurrentDictionary<string, JToken>(ConfigurationMixin.DefaultKeyComparer);
        _serializer.Populate(reader, _values);
    }

    /// <summary>
    /// Gets the number of configuration values currently loaded.
    /// </summary>
    protected int Count => _values.Count;

    /// <inheritdoc />
    protected override IEnumerable<string> InternalSafeGetAvailableParts() => _values.Keys;

    /// <inheritdoc />
    protected override bool InternalSafeExist(string key) => _values.ContainsKey(key);

    /// <inheritdoc />
    protected override TPocoType InternalSafeGet<TPocoType>(
        string key,
        Lazy<TPocoType> defaultValue
    )
    {
        if (_values.TryGetValue(key, out var value))
        {
            return value.ToObject<TPocoType>() ?? throw new InvalidOperationException();
        }

        var inst = defaultValue.Value;
        Set(key, inst);
        return inst;
    }

    /// <inheritdoc />
    protected override void InternalSafeSave<TPocoType>(string key, TPocoType value)
    {
        using var stream = new MemoryStream();
        using var writer = new StreamWriter(stream);
        using var jsonTextWriter = new JsonTextWriter(writer);
        jsonTextWriter.Formatting = _serializer.Formatting;
        _serializer.Serialize(jsonTextWriter, value, typeof(TPocoType));
        writer.Flush();
        stream.Position = 0;
        using var jsonReader = new JsonTextReader(new StreamReader(stream));
        var jValue =
            _serializer.Deserialize<JToken>(jsonReader) ?? throw new InvalidOperationException();
        _values.AddOrUpdate(key, jValue, (_, _) => jValue);
        _onNeedToSave.OnNext(Unit.Default);
    }

    /// <inheritdoc />
    protected override void InternalSafeRemove(string key)
    {
        if (_values.TryRemove(key, out _))
        {
            _onNeedToSave.OnNext(Unit.Default);
        }
    }

    private void InternalSaveChanges(Unit unit)
    {
        using (_sync.EnterScope())
        {
            try
            {
                using var stream = BeginSaveChanges();
                using var file = new StreamWriter(stream);
                if (_sortKeysInFile)
                {
                    _serializer.Serialize(file, new SortedDictionary<string, JToken>(_values));
                }
                else
                {
                    _serializer.Serialize(file, _values);
                }

                // this is to reduce file corruption
                if (stream is FileStream fileStream)
                {
                    fileStream.Flush(true);
                }
            }
            catch (Exception e)
            {
                var ex = InternalPublishError(
                    new ConfigurationException($"Error to serialize configuration and save it", e)
                );
                if (!_deferredFlush)
                {
                    throw ex;
                }
            }
            finally
            {
                EndSaveChanges();
            }
        }
    }

    /// <summary>
    /// Completes a save operation after the JSON payload has been written.
    /// </summary>
    protected abstract void EndSaveChanges();

    /// <summary>
    /// Begins a save operation and returns the destination stream.
    /// </summary>
    /// <returns>The stream used to write configuration data.</returns>
    protected abstract Stream BeginSaveChanges();

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _onNeedToSave.Dispose();
            _saveSubscribe.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore()
    {
        await CastAndDispose(_onNeedToSave);
        await CastAndDispose(_saveSubscribe);

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
}
