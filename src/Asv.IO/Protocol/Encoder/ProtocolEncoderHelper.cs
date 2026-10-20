using System;
using System.Collections.Generic;

namespace Asv.IO;

public static class ProtocolEncoderHelper
{
    /// <summary>
    /// Registers a factory for a protocol's outbound stream encoder. Parser registration is separate.
    /// </summary>
    public static void RegisterEncoder(
        this IProtocolParserBuilder builder,
        ProtocolInfo info,
        EncoderFactoryDelegate factory
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(info);
        ArgumentNullException.ThrowIfNull(factory);
        if (builder is not IProtocolEncoderBuilder encoders)
        {
            throw new NotSupportedException(
                "This protocol builder does not support custom encoders."
            );
        }

        encoders.Register(info, factory);
    }

    /// <summary>
    /// Creates an encoder for a new outbound stream, falling back to the existing message serializer.
    /// </summary>
    /// <remarks>Retain this instance for the stream lifetime; do not call this method for every message.</remarks>
    public static IProtocolEncoder CreateEncoder(this IProtocolContext context, string protocolId)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(protocolId);
        return
            context is IProtocolEncoderContext encoders
            && encoders.EncoderFactory.TryGetValue(protocolId, out var factory)
            ? factory(context)
                ?? throw new InvalidOperationException("An encoder factory returned null.")
            : DefaultProtocolEncoder.Instance;
    }
}

/// <summary>Accessed only by the owning endpoint's serialized send path.</summary>
internal sealed class ProtocolEncoderSet(IProtocolContext context)
{
    private readonly Dictionary<string, IProtocolEncoder> _encoders = new();

    public IProtocolEncoder Get(string protocolId)
    {
        if (_encoders.TryGetValue(protocolId, out var encoder))
        {
            return encoder;
        }

        encoder = context.CreateEncoder(protocolId);
        _encoders.Add(protocolId, encoder);
        return encoder;
    }
}
