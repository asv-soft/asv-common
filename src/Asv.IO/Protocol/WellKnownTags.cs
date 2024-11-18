using System;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Asv.IO;

public static class WellKnownTags
{
    public const string PortIdTag = "PortId";
    public const string PortNameTag = "PortName";
    public const string ConnectionIdTag = "ConnectionId";

    public static string? GetPortId(this ProtocolTags tags)
    {
        return tags[PortIdTag];
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
        return tags[ConnectionIdTag];
    }
}



public sealed class ProtocolTags
{
    private TagList _tagList = new();
    
    public void Clear()
    {
        throw new System.NotImplementedException();
    }

    public string? this[string tagName]
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }
    
    public void CopyTo(ProtocolTags tags)
    {
        throw new NotImplementedException();
    }

    
}

