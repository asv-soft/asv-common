using System;
using System.Diagnostics.CodeAnalysis;
using ObservableCollections;

namespace Asv.IO;

/// <summary>
/// Exposes discovered client devices and separates protocol address extraction from live device attribution.
/// </summary>
/// <remarks>
/// <para>
/// Address extraction can succeed without a discovered device. <see cref="TryIdentify"/> instead resolves
/// a source address to an existing discovery binding; it does not run a factory's discovery classifier.
/// None of these queries discovers or updates a device or refreshes its inactivity timeout.
/// </para>
/// <para>
/// Dispose the explorer to stop discovery and release the devices it owns. The underlying protocol
/// connection remains the caller's responsibility.
/// </para>
/// </remarks>
/// <example>
/// With factories that support the message's protocol family, extraction can succeed before discovery.
/// The second query succeeds only while a discovered device has a binding for the same source.
/// <code>
/// static void InspectSource(IDeviceExplorer explorer, IProtocolMessage message)
/// {
///     if (explorer.TryGetSourceAddress(message, out var address))
///     {
///         Console.WriteLine($"Source: {address.Family}/{address.Value}");
///     }
///     if (explorer.TryIdentify(message, out var deviceId))
///     {
///         Console.WriteLine($"Discovered device: {deviceId}");
///     }
/// }
/// </code>
/// </example>
public interface IDeviceExplorer : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Gets all currently discovered devices, including devices whose initialization is incomplete or failed.
    /// </summary>
    /// <value>A live observable dictionary keyed by the discovered device identities.</value>
    /// <remarks>
    /// Entries remain until discovery removes them or the explorer is disposed. Membership does not
    /// guarantee a current connection, successful initialization, or an address binding for lookup.
    /// </remarks>
    IReadOnlyObservableDictionary<DeviceId, IClientDevice> Devices { get; }

    /// <summary>
    /// Gets discovered devices that have reached <see cref="ClientDeviceState.Complete"/> at least once.
    /// </summary>
    /// <value>A live observable list populated on each registered device's first completed initialization.</value>
    /// <remarks>
    /// A device stays in this list until it is removed from <see cref="Devices"/> or the explorer is
    /// disposed. Later state changes do not remove it; this is not a continuously filtered readiness list.
    /// </remarks>
    IReadOnlyObservableList<IClientDevice> InitializedDevices { get; }

    /// <summary>
    /// Tries to attribute a message's source address to a currently discovered device.
    /// </summary>
    /// <param name="message">The message whose protocol source address is to be resolved.</param>
    /// <param name="deviceId">The discovered identity on success; otherwise, <see langword="null"/>.</param>
    /// <returns>
    /// <see langword="true"/> when the source has a current discovery binding; otherwise,
    /// <see langword="false"/>, including for an unknown, removed, or unbound source or a disposed explorer.
    /// </returns>
    /// <remarks>
    /// Initialization need not have reached <see cref="ClientDeviceState.Complete"/>. This is a lookup
    /// only: it does not invoke <see cref="IClientDeviceFactory.TryIdentify"/>, create or update devices,
    /// or refresh their inactivity timeout. A message need not itself be a discovery message.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="message"/> is <see langword="null"/>.</exception>
    /// <seealso cref="TryGetSourceAddress"/>
    bool TryIdentify(IProtocolMessage message, [NotNullWhen(true)] out DeviceId? deviceId);

    /// <summary>
    /// Tries to extract a canonical protocol-family source address without consulting discovered devices.
    /// </summary>
    /// <param name="message">Any protocol message supported by an address-extracting factory.</param>
    /// <param name="address">The valid canonical source address on success; otherwise, the invalid default value.</param>
    /// <returns><see langword="true"/> if an address can be extracted; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// Extraction does not require discovery or initialization and does not refresh device liveness.
    /// The default interface implementation validates <paramref name="message"/> and returns
    /// <see langword="false"/> with a default address, preserving compatibility for implementations
    /// without address support. <see cref="DeviceExplorer"/> delegates to its ordered factories,
    /// including after disposal.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="message"/> is <see langword="null"/>.</exception>
    bool TryGetSourceAddress(IProtocolMessage message, out DeviceAddress address)
    {
        ArgumentNullException.ThrowIfNull(message);
        address = default;
        return false;
    }

    /// <summary>
    /// Tries to derive a canonical protocol-family address from a device identity without consulting discovered devices.
    /// </summary>
    /// <param name="deviceId">The identity to convert; it need not belong to a discovered device.</param>
    /// <param name="address">The valid canonical device address on success; otherwise, the invalid default value.</param>
    /// <returns><see langword="true"/> if the identity can be converted; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// Conversion does not discover, initialize, or update a device or refresh its inactivity timeout.
    /// The default interface implementation validates <paramref name="deviceId"/> and returns
    /// <see langword="false"/> with a default address. <see cref="DeviceExplorer"/> delegates to its
    /// ordered factories, including after disposal.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="deviceId"/> is <see langword="null"/>.</exception>
    /// <seealso cref="TryGetSourceAddress"/>
    bool TryGetDeviceAddress(DeviceId deviceId, out DeviceAddress address)
    {
        ArgumentNullException.ThrowIfNull(deviceId);
        address = default;
        return false;
    }
}
