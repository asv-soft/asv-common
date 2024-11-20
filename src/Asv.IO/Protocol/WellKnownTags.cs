using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Diagnostics;

namespace Asv.IO;

public static class WellKnownTags
{
    public const string PortIdTag = "PortId";
    public const string PortNameTag = "PortName";
    public const string ConnectionIdTag = "ConnectionId";

    public static string? GetPortId(this ProtocolTags tags)
    {
        return (string?)tags[PortIdTag];
    }
    public static void SetPortId(this ProtocolTags tags, string id)
    {
        tags[PortIdTag] = id;
    }
    
    
    public static void SetPortName(this ProtocolTags tags, string name)
    {
        tags[PortNameTag] = name;
    }
    
    public static void SetConnectionId(this ProtocolTags tags,string id)
    {
        tags[ConnectionIdTag] = id;
    }

    public static string? GetConnectionId(this ProtocolTags tags)
    {
        return (string?)tags[ConnectionIdTag];
    }
}



public sealed class ProtocolTags
{
    private readonly HybridDictionary _tagList = new(8, true);
    
    public void Clear()
    {
        _tagList.Clear();
    }

    public object? this[string tagName]
    {
        get => _tagList[tagName];
        set => _tagList[tagName] = value;
    }

    public void AddRange(ProtocolTags tags)
    { 
        foreach (DictionaryEntry tag in tags._tagList)
        {
            _tagList[tag.Key] = tag.Value;
        }
    }

    
}

