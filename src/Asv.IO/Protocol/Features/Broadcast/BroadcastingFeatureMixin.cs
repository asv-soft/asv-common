using System.Runtime.CompilerServices;

namespace Asv.IO;

public static class BroadcastingFeatureMixin
{
    private const string BroadCastTag = "broadcast";

    public static void RegisterBroadcastFeature<TMessage>(
        this IProtocolFeatureBuilder builder,
        BroadcastingFeatureOptions? options = null
    )
    {
        builder.Register(
            new BroadcastingFeature<TMessage>(options ?? BroadcastingFeatureOptions.Default)
        );
    }

    public static void RegisterBroadcastFeature<TMessage>(
        this IProtocolFeatureBuilder builder,
        bool isLoopProtectionEnabled
    )
    {
        builder.Register(new BroadcastingFeature<TMessage>(isLoopProtectionEnabled));
    }

    public static void RegisterBroadcastAllFeature(
        this IProtocolFeatureBuilder builder,
        BroadcastingFeatureOptions? options = null
    )
    {
        builder.Register(
            new BroadcastingFeature<IProtocolMessage>(options ?? BroadcastingFeatureOptions.Default)
        );
    }

    public static void RegisterBroadcastAllFeature(
        this IProtocolFeatureBuilder builder,
        bool isLoopProtectionEnabled
    )
    {
        builder.Register(new BroadcastingFeature<IProtocolMessage>(isLoopProtectionEnabled));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void MarkBroadcast(this ISupportTag src, IProtocolConnection connection)
    {
        src.Tags[BroadCastTag] = connection.Id;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool CheckBroadcast(this ISupportTag src, IProtocolConnection connection)
    {
        return src.Tags[BroadCastTag]?.Equals(connection.Id) ?? false;
    }
}
