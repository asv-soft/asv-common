using System;
using System.Collections.Generic;
using R3;

namespace Asv.Cfg
{
    public interface IConfiguration: IConfigurationReader, IDisposable
    {
        void Set<TPocoType>(string key, TPocoType value);
        void Remove(string key);
        Observable<ConfigurationException> OnError { get; }
        Observable<KeyValuePair<string, object?>> OnChanged { get; }
    }
}
