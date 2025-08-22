using System;
using System.Collections.Generic;

namespace Asv.Cfg;

public interface IConfigurationReader
{
    IEnumerable<string> ReservedParts { get; }
    IEnumerable<string> AvailableParts { get; }
    bool Exist(string key);
    TPocoType Get<TPocoType>(string key, Lazy<TPocoType> defaultValue);
}