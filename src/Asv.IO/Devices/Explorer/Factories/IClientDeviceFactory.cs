using System;
using System.Collections.Immutable;

namespace Asv.IO;

/// <summary>
/// Classifies discovery messages, creates and updates client devices, and optionally extracts canonical addresses.
/// </summary>
/// <remarks>
/// Discovery classification and address extraction serve different purposes. A factory may recognize only
/// a discovery packet in <see cref="TryIdentify"/> while extracting the same source address from other
/// packet types in <see cref="TryGetSourceAddress"/>. Factories without address support can still discover
/// devices, but those devices have no address-based lookup unless another supported extraction supplies a binding.
/// </remarks>
public interface IClientDeviceFactory
{
    /// <summary>
    /// Gets the factory's priority for discovery and address extraction.
    /// </summary>
    /// <value>An ordering key; smaller values are consulted first.</value>
    /// <remarks>
    /// <see cref="DeviceExplorer"/> snapshots factories in ascending order at construction and preserves
    /// registration order for equal keys. The first successful applicable factory wins.
    /// </remarks>
    int Order { get; }

    /// <summary>
    /// Tries to classify a discovery message and obtain the device identity it announces.
    /// </summary>
    /// <param name="message">The received packet to consider for discovery.</param>
    /// <param name="deviceId">The announced identity on success; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if this factory recognizes a discovery message; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// Classification is independent of the explorer's current catalog. A successful result must supply
    /// a non-null identity; the explorer ignores successful results with a null identity. This method
    /// does not mean that the device has already been discovered or initialized. Unlike
    /// <see cref="IDeviceExplorer.TryIdentify"/>, it is the input to discovery rather than a live lookup.
    /// </remarks>
    bool TryIdentify(IProtocolMessage message, out DeviceId? deviceId);

    /// <summary>
    /// Tries to extract a canonical protocol-family source address from a message without discovering a device.
    /// </summary>
    /// <param name="message">A protocol message, which need not be a discovery packet.</param>
    /// <param name="address">The canonical source address on success; otherwise, the invalid default value.</param>
    /// <returns><see langword="true"/> when this factory supports the source address; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// <para>
    /// Implement this as a pure extraction that does not create or update devices or refresh their liveness.
    /// Use the same canonical strings as <see cref="TryGetDeviceAddress"/>. The explorer skips successful
    /// results with invalid/default addresses and continues with the next factory.
    /// </para>
    /// <para>
    /// The default implementation returns <see langword="false"/> and a default address without inspecting
    /// the input. It adds no null validation, allowing existing factories to remain compatible.
    /// </para>
    /// </remarks>
    bool TryGetSourceAddress(IProtocolMessage message, out DeviceAddress address)
    {
        address = default;
        return false;
    }

    /// <summary>
    /// Tries to derive a canonical protocol-family address from an identity without requiring a discovered device.
    /// </summary>
    /// <param name="deviceId">An identity to convert, including one obtained outside the explorer.</param>
    /// <param name="address">The canonical device address on success; otherwise, the invalid default value.</param>
    /// <returns><see langword="true"/> when this factory supports the identity; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// <para>
    /// Implement this as a pure conversion using the same canonical strings as
    /// <see cref="TryGetSourceAddress"/>. The explorer skips invalid/default successful results.
    /// During discovery, this hook on the classifying factory can supply an address when source
    /// extraction is unavailable; conflicting valid source and identity addresses prevent a binding.
    /// </para>
    /// <para>
    /// The default implementation returns <see langword="false"/> and a default address without inspecting
    /// or validating the input, preserving compatibility for factories without address support.
    /// </para>
    /// </remarks>
    bool TryGetDeviceAddress(DeviceId deviceId, out DeviceAddress address)
    {
        address = default;
        return false;
    }

    /// <summary>
    /// Applies accepted discovery traffic to an existing device.
    /// </summary>
    /// <param name="device">The previously created device to update.</param>
    /// <param name="message">The accepted discovery message used for the update.</param>
    /// <remarks>
    /// For an existing address binding, the explorer calls the factory that created the owning device.
    /// Without such a binding, an existing identity is updated by the factory classifying the current
    /// message. Implementations must support the device and message types used in their update path.
    /// </remarks>
    void UpdateDevice(IClientDevice device, IProtocolMessage message);

    /// <summary>
    /// Creates a client device for a successfully classified discovery message.
    /// </summary>
    /// <param name="message">The discovery message accepted by <see cref="TryIdentify"/>.</param>
    /// <param name="deviceId">The identity returned by discovery classification.</param>
    /// <param name="context">The shared connection, logging, time, and metrics services available to the device.</param>
    /// <param name="extenders">The registered extensions to pass to the device's initialization workflow.</param>
    /// <returns>A new device that the explorer will initialize, register, and own.</returns>
    /// <remarks>
    /// The explorer calls <see cref="IClientDevice.Initialize"/> after this method returns. The shared
    /// context and extender objects are passed through; creating a device does not transfer ownership
    /// of the protocol connection. Extenders retain their own microservice ownership obligations as
    /// described by <see cref="IClientDeviceExtender"/>.
    /// </remarks>
    IClientDevice CreateDevice(
        IProtocolMessage message,
        DeviceId deviceId,
        IMicroserviceContext context,
        ImmutableArray<IClientDeviceExtender> extenders
    );
}

/// <summary>
/// Defines conventional priority values for client-device factory registration.
/// </summary>
public static class ClientDeviceFactory
{
    /// <summary>
    /// The conventional normal priority, <c>0</c>.
    /// </summary>
    public const int DefaultOrder = 0;

    /// <summary>
    /// The lowest-priority ordering key, <see cref="int.MaxValue"/>, consulted after smaller keys.
    /// </summary>
    public const int MinimumOrder = int.MaxValue;
}

/// <summary>
/// Adapts untyped discovery operations to strongly typed classification, update, and creation hooks.
/// </summary>
/// <typeparam name="TMessageBase">The message type accepted for discovery, updates, and creation.</typeparam>
/// <typeparam name="TDeviceBase">The device type passed to the update hook and returned by the creation hook.</typeparam>
/// <typeparam name="TDeviceId">The identity type produced by classification and passed to creation.</typeparam>
/// <remarks>
/// <para>
/// The discovery methods gate messages by <typeparamref name="TMessageBase"/> and cast the corresponding
/// device or identity to the declared type. Address hooks are independent: they receive any
/// <see cref="IProtocolMessage"/> or <see cref="DeviceId"/> and are not restricted by those type gates.
/// </para>
/// <para>
/// Override address hooks when ordinary telemetry and discovery packets share a source address.
/// Their defaults return <see langword="false"/> and a default address without inspecting the input.
/// </para>
/// </remarks>
public abstract class ClientDeviceFactory<TMessageBase, TDeviceBase, TDeviceId>
    : IClientDeviceFactory
    where TDeviceId : DeviceId
    where TDeviceBase : IClientDevice
    where TMessageBase : IProtocolMessage
{
    /// <inheritdoc cref="IClientDeviceFactory.Order"/>
    public abstract int Order { get; }

    /// <inheritdoc cref="IClientDeviceFactory.TryGetSourceAddress"/>
    /// <remarks>
    /// The base implementation returns <see langword="false"/> and a default address without null
    /// validation. Overrides may handle messages outside <typeparamref name="TMessageBase"/>.
    /// </remarks>
    public virtual bool TryGetSourceAddress(IProtocolMessage message, out DeviceAddress address)
    {
        address = default;
        return false;
    }

    /// <inheritdoc cref="IClientDeviceFactory.TryGetDeviceAddress"/>
    /// <remarks>
    /// The base implementation returns <see langword="false"/> and a default address without null
    /// validation or a <typeparamref name="TDeviceId"/> type gate.
    /// </remarks>
    public virtual bool TryGetDeviceAddress(DeviceId deviceId, out DeviceAddress address)
    {
        address = default;
        return false;
    }

    /// <inheritdoc cref="IClientDeviceFactory.TryIdentify"/>
    /// <remarks>
    /// Calls <see cref="InternalTryIdentify"/> only for <typeparamref name="TMessageBase"/> messages.
    /// Other message types, including a null input, return <see langword="false"/> and a null identity.
    /// A successful typed hook result is returned unchanged; implementations must supply a non-null identity.
    /// </remarks>
    public bool TryIdentify(IProtocolMessage message, out DeviceId? deviceId)
    {
        if (message is TMessageBase msg)
        {
            var res = InternalTryIdentify(msg, out var dev);
            if (res)
            {
                deviceId = dev;
                return true;
            }
        }
        deviceId = null;
        return false;
    }

    /// <summary>
    /// Classifies a supported message as discovery traffic and extracts its typed device identity.
    /// </summary>
    /// <param name="msg">The message already verified to be a <typeparamref name="TMessageBase"/>.</param>
    /// <param name="deviceId">The non-null announced identity on success; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> for accepted discovery traffic; otherwise, <see langword="false"/>.</returns>
    /// <remarks>This hook classifies the packet independently of any existing explorer catalog.</remarks>
    protected abstract bool InternalTryIdentify(TMessageBase msg, out TDeviceId? deviceId);

    /// <inheritdoc cref="IClientDeviceFactory.UpdateDevice"/>
    /// <remarks>
    /// Verifies the message type and casts <paramref name="device"/> before calling
    /// <see cref="InternalUpdateDevice"/>. This adapter does not validate null arguments separately.
    /// </remarks>
    /// <exception cref="InvalidOperationException"><paramref name="message"/> is non-null and is not a <typeparamref name="TMessageBase"/>.</exception>
    /// <exception cref="InvalidCastException"><paramref name="device"/> cannot be cast to <typeparamref name="TDeviceBase"/> after the message type is accepted.</exception>
    /// <exception cref="NullReferenceException"><paramref name="message"/> is <see langword="null"/>.</exception>
    public void UpdateDevice(IClientDevice device, IProtocolMessage message)
    {
        if (message is TMessageBase msg)
        {
            InternalUpdateDevice((TDeviceBase)device, msg);
            return;
        }
        throw new InvalidOperationException($"Unknown message type {message.GetType().Name}");
    }

    /// <summary>
    /// Updates a typed device from accepted discovery traffic.
    /// </summary>
    /// <param name="device">The existing device cast to <typeparamref name="TDeviceBase"/>.</param>
    /// <param name="msg">The discovery message verified to be a <typeparamref name="TMessageBase"/>.</param>
    protected abstract void InternalUpdateDevice(TDeviceBase device, TMessageBase msg);

    /// <inheritdoc cref="IClientDeviceFactory.CreateDevice"/>
    /// <remarks>
    /// Verifies the message type and casts <paramref name="deviceId"/> before delegating to
    /// <see cref="InternalCreateDevice"/>. Context and extenders are forwarded unchanged.
    /// </remarks>
    /// <exception cref="InvalidOperationException"><paramref name="message"/> is non-null and is not a <typeparamref name="TMessageBase"/>.</exception>
    /// <exception cref="InvalidCastException"><paramref name="deviceId"/> cannot be cast to <typeparamref name="TDeviceId"/> after the message type is accepted.</exception>
    /// <exception cref="NullReferenceException"><paramref name="message"/> is <see langword="null"/>.</exception>
    public IClientDevice CreateDevice(
        IProtocolMessage message,
        DeviceId deviceId,
        IMicroserviceContext context,
        ImmutableArray<IClientDeviceExtender> extenders
    )
    {
        if (message is TMessageBase msg)
        {
            return InternalCreateDevice(msg, (TDeviceId)deviceId, context, extenders);
        }
        throw new InvalidOperationException($"Unknown message type {message.GetType().Name}");
    }

    /// <summary>
    /// Creates the typed device after the discovery adapter has accepted the message type.
    /// </summary>
    /// <param name="msg">The discovery message verified to be a <typeparamref name="TMessageBase"/>.</param>
    /// <param name="deviceId">The discovery identity cast to <typeparamref name="TDeviceId"/>.</param>
    /// <param name="context">The shared services forwarded by the explorer.</param>
    /// <param name="extenders">The registered extensions forwarded for device initialization.</param>
    /// <returns>A new device for the explorer to initialize and own.</returns>
    /// <seealso cref="IClientDeviceFactory.CreateDevice"/>
    protected abstract TDeviceBase InternalCreateDevice(
        TMessageBase msg,
        TDeviceId deviceId,
        IMicroserviceContext context,
        ImmutableArray<IClientDeviceExtender> extenders
    );
}

/// <summary>
/// Manages the factories included when a device explorer is built.
/// </summary>
public interface IClientDeviceFactoryBuilder
{
    /// <summary>
    /// Adds a factory to the registration sequence used for discovery and address extraction.
    /// </summary>
    /// <param name="factory">The factory to register.</param>
    /// <remarks>Factories are ordered by <see cref="IClientDeviceFactory.Order"/> when the explorer is built.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="factory"/> is <see langword="null"/>.</exception>
    void Register(IClientDeviceFactory factory);

    /// <summary>
    /// Removes all factory registrations from the builder before an explorer is built.
    /// </summary>
    /// <remarks>Existing built explorers retain their own factory snapshots.</remarks>
    void Clear();
}
