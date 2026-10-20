using System.Collections.Immutable;

namespace Asv.IO;

/// <summary>
/// Optional encoder factories. Contexts without this capability use message serialization.
/// </summary>
public interface IProtocolEncoderContext
{
    ImmutableDictionary<string, EncoderFactoryDelegate> EncoderFactory { get; }
}
