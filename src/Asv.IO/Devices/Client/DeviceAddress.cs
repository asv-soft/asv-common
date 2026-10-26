using System;

namespace Asv.IO;

/// <summary>
/// Identifies a protocol source by its canonical protocol family and address, independently of a transport channel.
/// </summary>
/// <remarks>
/// <para>
/// Factories must use the same canonical strings when extracting an address from a message and from a
/// <see cref="DeviceId"/>. Both strings participate in ordinal, case-sensitive value equality. This type
/// neither normalizes case nor trims whitespace or reformats numeric components.
/// </para>
/// <para>
/// For example, the family <c>mavlink</c> and value <c>1:1</c> can identify a system/component source.
/// A transport port, device class, or protocol version is not a separate part of this identity.
/// Different transport channels can therefore refer to the same source.
/// </para>
/// <para>
/// The default value is invalid: its <see cref="Family"/> and <see cref="Value"/> are <see langword="null"/>
/// despite their non-nullable declarations. Address extraction methods return this value on failure;
/// callers must check their Boolean result before using the address.
/// </para>
/// </remarks>
/// <seealso cref="IDeviceExplorer.TryGetSourceAddress"/>
/// <seealso cref="IDeviceExplorer.TryGetDeviceAddress"/>
public readonly record struct DeviceAddress
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeviceAddress"/> struct.
    /// The protocol-family and source strings must already be canonical; they are stored without normalization.
    /// </summary>
    /// <param name="family">The nonempty protocol-family identifier shared by compatible factories.</param>
    /// <param name="value">The nonempty source address in that family's canonical format.</param>
    /// <exception cref="ArgumentNullException"><paramref name="family"/> or <paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="family"/> or <paramref name="value"/> is empty or consists only of whitespace.</exception>
    public DeviceAddress(string family, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(family);
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Family = family;
        Value = value;
    }

    /// <summary>
    /// Gets the protocol-family namespace used to interpret <see cref="Value"/>.
    /// </summary>
    /// <value>The original family string, or <see langword="null"/> for a default address.</value>
    public string Family { get; }

    /// <summary>
    /// Gets the canonical source address within <see cref="Family"/>.
    /// </summary>
    /// <value>The original address string, or <see langword="null"/> for a default address.</value>
    public string Value { get; }
}
