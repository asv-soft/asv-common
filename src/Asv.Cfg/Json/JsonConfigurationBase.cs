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

public abstract class JsonConfigurationBase : ConfigurationBase
{
    private readonly ConcurrentDictionary<string, JToken> _values;
    private readonly Subject<Unit> _onNeedToSave = new();
    private readonly JsonSerializer _serializer;
    private readonly Lock _sync = new();
    private readonly bool _sortKeysInFile;
    private readonly IDisposable _saveSubscribe;
    private readonly bool _deferredFlush;

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

    protected int Count => _values.Count;

    protected override IEnumerable<string> InternalSafeGetAvailableParts() => _values.Keys;

    protected override bool InternalSafeExist(string key) => _values.ContainsKey(key);

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

    protected abstract void EndSaveChanges();
    protected abstract Stream BeginSaveChanges();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _onNeedToSave.Dispose();
            _saveSubscribe.Dispose();
        }

        base.Dispose(disposing);
    }

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
