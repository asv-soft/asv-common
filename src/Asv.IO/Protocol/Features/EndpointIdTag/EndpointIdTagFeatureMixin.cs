using System.Runtime.CompilerServices;

namespace Asv.IO;

public static class EndpointIdTagFeatureMixin
{
    private const string TagId = "endpoint";
    
    public static void RegisterEndpointIdTagFeature(this IProtocolFeatureBuilder builder)
    {
        builder.Register(new EndpointIdTagFeature());
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetConnectionTag(this IProtocolEndpoint src)
    {
        src.Tags[TagId] = src.Id;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string? GetEndpointId(this IProtocolMessage src)
    {
        return (string?)src.Tags[TagId] ?? null;
    }

    public static void SetConnectionTag(this IProtocolEndpoint src, IProtocolMessage message)
    {
        message.Tags[TagId] = src.Tags[TagId];
    }
}