using System;
using System.Collections.Generic;
using System.IO.Packaging;
using System.Linq;
using R3;
using ZLogger;

namespace Asv.IO;

public class ReactivePropertyPackagePart : KvJsonAsvPackagePart, ISupportChanges
{
    private readonly ReactiveProperty<bool> _hasChanges;

    public ReactivePropertyPackagePart(Uri uriPart, string contentType, CompressionOption compression, AsvPackageContext context)
        : base(uriPart, contentType, compression, context)
    {
        _hasChanges = AddToDispose(new ReactiveProperty<bool>(false));
        AddToDispose(_hasChanges
            .DistinctUntilChanged()
            .Subscribe(x => Context.Publish(new HasChangesEvent(this, x))));
    }

    protected SortedDictionary<string, (Action<string>, Func<string>)> Props { get; } = new();

    public ReactiveProperty<T> AddProperty<T>(string key, T defaultValue, Func<string, T> load, Func<T, string> save)
    {
        var prop = AddToDispose(new ReactiveProperty<T>(defaultValue));
        AddToDispose(prop.DistinctUntilChanged()
            .Subscribe(_ => _hasChanges.Value = true));
        Props.Add(key, (str => prop.Value = load(str), () => save(prop.Value)));
        return prop;
    }


    public void Load()
    {
        try
        {
            Load(InternalLoad);
        }
        catch (Exception e)
        {
            Context.Logger.ZLogWarning($"Error to load params: {e.Message}");
        }
        _hasChanges.Value = false;
    }

    public override void Flush()
    {
        Save(Props.Select(x => new KeyValuePair<string, string>(x.Key, x.Value.Item2())));
        base.Flush();
        _hasChanges.Value = false;
    }

    private void InternalLoad(KeyValuePair<string, string> kv)
    {
        if (Props.TryGetValue(kv.Key, out var converter))
        {
            try
            {
                converter.Item1(kv.Value);
            }
            catch (Exception e)
            {
                Context.Logger.ZLogError($"Error to load property {kv.Key}: {e.Message}");
            }
        }
        else
        {
            Context.Logger.ZLogWarning($"Unknown property at file: {kv.Key}");
        }
    }

    public ReadOnlyReactiveProperty<bool> HasChanges => _hasChanges;
}