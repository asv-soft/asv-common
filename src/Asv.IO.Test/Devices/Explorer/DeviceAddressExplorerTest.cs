using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading.Tasks;
using Asv.Common;
using Asv.IO.Device;
using ObservableCollections;
using R3;
using Xunit;

namespace Asv.IO.Test.Devices.Explorer;

public class DeviceAddressExplorerTest
{
    [Fact]
    public void Discovery_LegacyFactoriesUseCurrentMatchingMessageFactoryForUpdates()
    {
        using var fixture = new Fixture(
            new LegacyTypedFactory<ExampleMessage1>(),
            new LegacyTypedFactory<ExampleMessage2>()
        );
        fixture.Connection.Receive(new ExampleMessage1 { SenderId = 7 });
        var original = Assert.IsType<AddressDevice>(Assert.Single(fixture.Explorer.Devices).Value);

        fixture.Connection.Receive(new ExampleMessage2 { SenderId = 7 });

        Assert.Same(original, Assert.Single(fixture.Explorer.Devices).Value);
        Assert.Equal(1, original.Updates);
        Assert.Equal("ExampleMessage2", original.LastUpdater);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Dispose_CompletionDuringOwnedDeviceDisposalCannotRestoreInitializedDevice(
        bool asyncDispose
    )
    {
        using var fixture = new Fixture(new AddressFactory());
        fixture.Connection.Receive(Discovery(7, "first"));
        var device = Assert.IsType<AddressDevice>(Assert.Single(fixture.Explorer.Devices).Value);
        device.BeforeDispose = device.Complete;
        Assert.Empty(fixture.Explorer.InitializedDevices);

        if (asyncDispose)
        {
            await fixture.Explorer.DisposeAsync();
        }
        else
        {
            fixture.Explorer.Dispose();
        }

        Assert.True(device.IsDisposed);
        Assert.Empty(fixture.Explorer.Devices);
        Assert.Empty(fixture.Explorer.InitializedDevices);
    }

    [Fact]
    public void Removal_LateCompletionCannotInitializeReplacementWithSameIdentity()
    {
        using var fixture = new Fixture(new AddressFactory());
        fixture.Connection.Receive(Discovery(7, "first"));
        var removed = Assert.IsType<AddressDevice>(Assert.Single(fixture.Explorer.Devices).Value);
        removed.KeepStateAliveOnDispose = true;
        try
        {
            fixture.Time.Advance(TimeSpan.FromMilliseconds(100));
            Assert.Empty(fixture.Explorer.Devices);
            fixture.Connection.Receive(Discovery(7, "first"));
            var replacement = Assert.IsType<AddressDevice>(
                Assert.Single(fixture.Explorer.Devices).Value
            );

            removed.Complete();

            Assert.Empty(fixture.Explorer.InitializedDevices);
            replacement.Complete();
            Assert.Same(replacement, Assert.Single(fixture.Explorer.InitializedDevices));
        }
        finally
        {
            removed.ReleaseState();
        }
    }

    [Fact]
    public void AddressFacades_ExtractBeforeDiscoveryWithoutCreatingOrRefreshingDevices()
    {
        var factory = new AddressFactory();
        using var fixture = new Fixture(factory);
        var telemetry = new ExampleMessage2 { SenderId = 7 };
        var offlineId = new AddressId(7, "offline");

        Assert.True(fixture.Explorer.TryGetSourceAddress(telemetry, out var source));
        Assert.Equal(new DeviceAddress("test", "7"), source);
        Assert.True(fixture.Explorer.TryGetDeviceAddress(offlineId, out var identity));
        Assert.Equal(new DeviceAddress("test", "7"), identity);
        Assert.Empty(fixture.Explorer.Devices);
        Assert.Equal(0, factory.DiscoveryCalls);
        Assert.False(fixture.Explorer.TryIdentify(telemetry, out _));
        fixture.Connection.Receive(Discovery(7, "first"));
        var device = Assert.IsType<AddressDevice>(Assert.Single(fixture.Explorer.Devices).Value);
        fixture.Time.Advance(TimeSpan.FromMilliseconds(90));

        Assert.True(fixture.Explorer.TryGetSourceAddress(telemetry, out _));
        Assert.True(fixture.Explorer.TryGetDeviceAddress(device.Id, out _));

        Assert.Equal(1, factory.DiscoveryCalls);
        Assert.Equal(0, device.Updates);
        fixture.Time.Advance(TimeSpan.FromMilliseconds(10));
        Assert.Empty(fixture.Explorer.Devices);
    }

    [Fact]
    public void AddressFacades_UseFirstValidSuccessfulFactoryInOrder()
    {
        var later = new AddressFactory { Priority = 10, Family = "later" };
        var earlier = new AddressFactory { Priority = -10, Family = "preferred" };
        var invalid = new AddressFactory { Priority = -20, ReturnDefaultAddress = true };
        using var fixture = new Fixture(later, earlier, invalid, new LegacyFactory());

        Assert.True(
            fixture.Explorer.TryGetSourceAddress(
                new ExampleMessage2 { SenderId = 7 },
                out var source
            )
        );
        Assert.Equal(new DeviceAddress("preferred", "7"), source);
        Assert.True(
            fixture.Explorer.TryGetDeviceAddress(new AddressId(7, "offline"), out var identity)
        );
        Assert.Equal(new DeviceAddress("preferred", "7"), identity);
        Assert.Equal(0, later.SourceCalls);
        Assert.Equal(0, later.DeviceAddressCalls);
        Assert.Empty(fixture.Explorer.Devices);
    }

    [Fact]
    public void AddressFacades_UnsupportedAndInvalidResultsReturnFalseAndDefault()
    {
        using var fixture = new Fixture(new LegacyFactory());
        Assert.False(
            fixture.Explorer.TryGetSourceAddress(
                new ExampleMessage2 { SenderId = 7 },
                out var source
            )
        );
        Assert.Equal(default, source);
        Assert.False(
            fixture.Explorer.TryGetDeviceAddress(new AddressId(7, "offline"), out var identity)
        );
        Assert.Equal(default, identity);
        using var invalidFixture = new Fixture(new AddressFactory { ReturnDefaultAddress = true });
        Assert.False(
            invalidFixture.Explorer.TryGetSourceAddress(
                new ExampleMessage2 { SenderId = 7 },
                out source
            )
        );
        Assert.Equal(default, source);
        Assert.False(
            invalidFixture.Explorer.TryGetDeviceAddress(new AddressId(7, "offline"), out identity)
        );
        Assert.Equal(default, identity);
    }

    [Fact]
    public void AddressFacades_NullInputsThrow()
    {
        using var fixture = new Fixture(new AddressFactory());
        Assert.Throws<ArgumentNullException>(() =>
            fixture.Explorer.TryGetSourceAddress(null!, out _)
        );
        Assert.Throws<ArgumentNullException>(() =>
            fixture.Explorer.TryGetDeviceAddress(null!, out _)
        );
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AddressFacades_PureExtractionRemainsAvailableAfterDisposal(bool asyncDispose)
    {
        using var fixture = new Fixture(new AddressFactory());
        if (asyncDispose)
        {
            await fixture.Explorer.DisposeAsync();
        }
        else
        {
            fixture.Explorer.Dispose();
        }

        Assert.True(
            fixture.Explorer.TryGetSourceAddress(
                new ExampleMessage2 { SenderId = 7 },
                out var source
            )
        );
        Assert.Equal(new DeviceAddress("test", "7"), source);
        Assert.True(
            fixture.Explorer.TryGetDeviceAddress(new AddressId(7, "offline"), out var identity)
        );
        Assert.Equal(new DeviceAddress("test", "7"), identity);
        Assert.False(fixture.Explorer.TryIdentify(new ExampleMessage2 { SenderId = 7 }, out _));
        Assert.Empty(fixture.Explorer.Devices);
    }

    [Fact]
    public void AddressFacades_ExistingExplorerImplementationsRetainCompatibleFalseDefaults()
    {
        using IDeviceExplorer explorer = new LegacyExplorer();
        Assert.False(
            explorer.TryGetSourceAddress(new ExampleMessage2 { SenderId = 7 }, out var source)
        );
        Assert.Equal(default, source);
        Assert.False(explorer.TryGetDeviceAddress(new AddressId(7, "offline"), out var identity));
        Assert.Equal(default, identity);
    }

    [Fact]
    public void Lookup_NonDiscoveryMessageResolvesBeforeInitializationCompletes()
    {
        using var fixture = new Fixture(new AddressFactory());
        fixture.Connection.Receive(Discovery(7, "first"));
        var device = Assert.Single(fixture.Explorer.Devices).Value;

        Assert.Empty(fixture.Explorer.InitializedDevices);
        Assert.True(fixture.Explorer.TryIdentify(new ExampleMessage2 { SenderId = 7 }, out var id));
        Assert.Same(device.Id, id);
        Assert.Equal(ClientDeviceState.InProgress, device.State.CurrentValue);
    }

    [Fact]
    public void Lookup_UnknownOrUndiscoveredAddressDoesNotDiscover()
    {
        var factory = new AddressFactory();
        using var fixture = new Fixture(factory);

        Assert.False(fixture.Explorer.TryIdentify(Discovery(7, "first"), out var id));
        Assert.Null(id);
        Assert.Empty(fixture.Explorer.Devices);
        Assert.Equal(0, factory.DiscoveryCalls);
        fixture.Connection.Receive(Discovery(7, "first"));
        Assert.False(fixture.Explorer.TryIdentify(new ExampleMessage2 { SenderId = 8 }, out id));
        Assert.Null(id);
        Assert.True(fixture.Explorer.TryIdentify(Discovery(7, "other"), out _));
        Assert.Equal(1, factory.DiscoveryCalls);
        Assert.Equal(
            0,
            Assert.IsType<AddressDevice>(Assert.Single(fixture.Explorer.Devices).Value).Updates
        );
    }

    [Fact]
    public void Lookup_NullInputThrows()
    {
        using var fixture = new Fixture(new AddressFactory());
        Assert.Throws<ArgumentNullException>(() => fixture.Explorer.TryIdentify(null!, out _));
    }

    [Fact]
    public void Lookup_NoRegisteredFactoryReturnsFalse()
    {
        using var fixture = new Fixture();
        fixture.Connection.Receive(Discovery(7, "first"));
        Assert.False(
            fixture.Explorer.TryIdentify(new ExampleMessage2 { SenderId = 7 }, out var id)
        );
        Assert.Null(id);
        Assert.Empty(fixture.Explorer.Devices);
    }

    [Fact]
    public void Discovery_FactoriesWithoutAddressSupportStillCreateDevices()
    {
        IClientDeviceFactory factory = new LegacyFactory();
        Assert.False(factory.TryGetSourceAddress(Discovery(7, "first"), out var address));
        Assert.Equal(default, address);
        Assert.False(factory.TryGetDeviceAddress(new AddressId(7, "first"), out address));
        var generic = new ExampleDeviceFactory(new ExampleDeviceConfig());
        Assert.False(generic.TryGetSourceAddress(Discovery(7, "first"), out _));
        Assert.False(generic.TryGetDeviceAddress(new ExampleDeviceId("example", 7), out _));
        using var fixture = new Fixture(factory);

        fixture.Connection.Receive(Discovery(7, "first"));

        Assert.Single(fixture.Explorer.Devices);
        Assert.False(fixture.Explorer.TryIdentify(Discovery(7, "first"), out var id));
        Assert.Null(id);
    }

    [Fact]
    public void Lookup_DoesNotRefreshLivenessAndAddressCanBeReacquired()
    {
        using var fixture = new Fixture(new AddressFactory());
        fixture.Connection.Receive(Discovery(7, "first"));
        var original = Assert.Single(fixture.Explorer.Devices).Value;
        fixture.Time.Advance(TimeSpan.FromMilliseconds(90));
        Assert.True(fixture.Explorer.TryIdentify(new ExampleMessage2 { SenderId = 7 }, out _));

        fixture.Time.Advance(TimeSpan.FromMilliseconds(10));

        Assert.Empty(fixture.Explorer.Devices);
        Assert.True(Assert.IsType<AddressDevice>(original).IsDisposed);
        Assert.False(
            fixture.Explorer.TryIdentify(new ExampleMessage2 { SenderId = 7 }, out var removed)
        );
        Assert.Null(removed);
        fixture.Connection.Receive(Discovery(7, "second"));
        var replacement = Assert.Single(fixture.Explorer.Devices).Value;
        Assert.NotSame(original, replacement);
        Assert.True(
            fixture.Explorer.TryIdentify(new ExampleMessage2 { SenderId = 7 }, out var reacquired)
        );
        Assert.Same(replacement.Id, reacquired);
        fixture.Time.Advance(TimeSpan.FromMilliseconds(100));
        Assert.Empty(fixture.Explorer.Devices);
        Assert.False(fixture.Explorer.TryIdentify(new ExampleMessage2 { SenderId = 7 }, out _));
    }

    [Fact]
    public void Discovery_SameAddressAcrossChannelsAndLocalIdsUpdatesOwningDevice()
    {
        var owner = new AddressFactory { LocalFilter = "first", Name = "owner" };
        var other = new AddressFactory { LocalFilter = "second", Name = "other" };
        using var fixture = new Fixture(owner, other);
        fixture.Connection.Receive(Discovery(7, "first"));
        var original = Assert.IsType<AddressDevice>(Assert.Single(fixture.Explorer.Devices).Value);
        fixture.Time.Advance(TimeSpan.FromMilliseconds(90));

        fixture.Connection.Receive(Discovery(7, "second"));

        Assert.Same(original, Assert.Single(fixture.Explorer.Devices).Value);
        Assert.Equal(1, original.Updates);
        Assert.Equal("owner", original.LastUpdater);
        fixture.Time.Advance(TimeSpan.FromMilliseconds(10));
        Assert.Single(fixture.Explorer.Devices);
        Assert.True(fixture.Explorer.TryIdentify(new ExampleMessage2 { SenderId = 7 }, out var id));
        Assert.Same(original.Id, id);
    }

    [Fact]
    public void Discovery_DifferentAddressesWithSameClassAndHashCoexistAndExpireIndependently()
    {
        using var fixture = new Fixture(new AddressFactory());
        fixture.Connection.Receive(Discovery(7, "first"));
        fixture.Time.Advance(TimeSpan.FromMilliseconds(50));
        fixture.Connection.Receive(Discovery(8, "first"));

        Assert.Equal(2, fixture.Explorer.Devices.Count);
        Assert.True(
            fixture.Explorer.TryIdentify(new ExampleMessage2 { SenderId = 7 }, out var first)
        );
        Assert.True(
            fixture.Explorer.TryIdentify(new ExampleMessage2 { SenderId = 8 }, out var second)
        );
        Assert.NotSame(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());

        // Preserve the public base contract while Explorer uses full derived identity internally.
        Assert.True(first.Equals(second));
        fixture.Time.Advance(TimeSpan.FromMilliseconds(50));
        Assert.Single(fixture.Explorer.Devices);
        Assert.False(fixture.Explorer.TryIdentify(new ExampleMessage2 { SenderId = 7 }, out _));
        Assert.True(
            fixture.Explorer.TryIdentify(new ExampleMessage2 { SenderId = 8 }, out var remaining)
        );
        Assert.Same(second, remaining);
        fixture.Time.Advance(TimeSpan.FromMilliseconds(50));
        Assert.Empty(fixture.Explorer.Devices);
    }

    [Fact]
    public void Lookup_FirstSuccessfulSourceExtractorInOrderIsCanonical()
    {
        var later = new AddressFactory { Priority = 10, SourceFamily = "later" };
        var earlier = new AddressFactory { Priority = -10, LocalFilter = "never" };
        using var fixture = new Fixture(later, earlier);
        fixture.Connection.Receive(Discovery(7, "first"));

        Assert.True(fixture.Explorer.TryIdentify(new ExampleMessage2 { SenderId = 7 }, out var id));
        Assert.Same(Assert.Single(fixture.Explorer.Devices).Key, id);
        Assert.Equal(0, later.SourceCalls);
        Assert.True(earlier.SourceCalls >= 2);
    }

    [Fact]
    public void Discovery_ConflictingSourceAndDeviceAddressesRemainUnbound()
    {
        using var fixture = new Fixture(new AddressFactory { SourceFamily = "conflicting" });
        fixture.Connection.Receive(Discovery(7, "first"));
        fixture.Connection.Receive(Discovery(7, "second"));

        Assert.Equal(2, fixture.Explorer.Devices.Count);
        Assert.False(
            fixture.Explorer.TryIdentify(new ExampleMessage2 { SenderId = 7 }, out var id)
        );
        Assert.Null(id);
    }

    [Fact]
    public void Discovery_DeviceAddressHookBindsWhenSourceIsUnavailable()
    {
        using var fixture = new Fixture(new AddressFactory { DiscoveryHasSource = false });
        fixture.Connection.Receive(Discovery(7, "first"));

        Assert.False(fixture.Explorer.TryIdentify(Discovery(7, "first"), out _));
        Assert.True(fixture.Explorer.TryIdentify(new ExampleMessage2 { SenderId = 7 }, out var id));
        Assert.Same(Assert.Single(fixture.Explorer.Devices).Key, id);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Dispose_ClearsAddressBindingsAndDevices(bool asyncDispose)
    {
        using var fixture = new Fixture(new AddressFactory());
        fixture.Connection.Receive(Discovery(7, "first"));
        var device = Assert.IsType<AddressDevice>(Assert.Single(fixture.Explorer.Devices).Value);
        Assert.True(fixture.Explorer.TryIdentify(new ExampleMessage2 { SenderId = 7 }, out _));
        if (asyncDispose)
        {
            await fixture.Explorer.DisposeAsync();
        }
        else
        {
            fixture.Explorer.Dispose();
        }

        Assert.False(
            fixture.Explorer.TryIdentify(new ExampleMessage2 { SenderId = 7 }, out var id)
        );
        Assert.Null(id);
        Assert.Empty(fixture.Explorer.Devices);
        Assert.Empty(fixture.Explorer.InitializedDevices);
        Assert.True(device.IsDisposed);
        Assert.Equal(0, fixture.Time.ActiveTimers);
        fixture.Connection.Receive(Discovery(7, "second"));
        Assert.Empty(fixture.Explorer.Devices);
    }

    [Theory]
    [InlineData(null, "7")]
    [InlineData("", "7")]
    [InlineData("test", null)]
    [InlineData("test", "")]
    public void DeviceAddress_RejectsMissingParts(string? family, string? value)
    {
        Assert.ThrowsAny<ArgumentException>(() => new DeviceAddress(family!, value!));
    }

    [Fact]
    public void DeviceAddress_UsesOrdinalValueEquality()
    {
        var addresses = new HashSet<DeviceAddress>
        {
            new("test", "7"),
            new("test", "7"),
            new("Test", "7"),
            new("test", "07"),
        };
        Assert.Equal(3, addresses.Count);
        Assert.DoesNotContain(default(DeviceAddress), addresses);
        var address = new DeviceAddress("test", "7");
        Assert.Equal("test", address.Family);
        Assert.Equal("7", address.Value);
    }

    private static ExampleMessage1 Discovery(byte sender, string local)
    {
        var message = new ExampleMessage1 { SenderId = sender };
        message.Tags.Add("channel", local);
        return message;
    }

    private sealed class Fixture : IDisposable
    {
        public DeviceLifetimeConnection Connection { get; } = new();
        public DeviceLifetimeTimeProvider Time { get; } = new();
        public IDeviceExplorer Explorer { get; }

        public Fixture(params IClientDeviceFactory[] factories)
        {
            Explorer = DeviceExplorer.Create(
                Connection,
                builder =>
                {
                    builder.SetTimeProvider(Time);
                    builder.SetConfig(
                        new ClientDeviceBrowserConfig
                        {
                            DeviceTimeoutMs = 100,
                            DeviceCheckIntervalMs = 10,
                        }
                    );
                    foreach (var factory in factories)
                    {
                        builder.Factories.Register(factory);
                    }
                }
            );
        }

        public void Dispose()
        {
            Explorer.Dispose();
            Connection.Dispose();
        }
    }

    private sealed class AddressId(byte remote, string local) : DeviceId("test")
    {
        public byte Remote { get; } = remote;
        public string Local { get; } = local;

        public override bool Equals(object? obj) =>
            obj is AddressId other && Remote == other.Remote && Local == other.Local;

        public override int GetHashCode() => 42;

        public override string AsString() => $"test.{Remote}.{Local}";
    }

    private sealed class AddressDevice(DeviceId id) : AsyncDisposableOnce, IClientDevice
    {
        private readonly ReactiveProperty<string?> _name = new("test");
        private readonly ReactiveProperty<ClientDeviceState> _state = new(
            ClientDeviceState.Uninitialized
        );

        public void Initialize() => _state.Value = ClientDeviceState.InProgress;

        public DeviceId Id => id;
        public ReadOnlyReactiveProperty<string?> Name => _name;
        public ReadOnlyReactiveProperty<ClientDeviceState> State => _state;
        public ILinkIndicator Link => throw new NotSupportedException();
        public ImmutableArray<IMicroserviceClient> Microservices => [];
        public int Updates { get; set; }
        public string? LastUpdater { get; set; }
        public Action? BeforeDispose { get; set; }
        public bool KeepStateAliveOnDispose { get; set; }

        public void Complete() => _state.Value = ClientDeviceState.Complete;

        public void ReleaseState() => _state.Dispose();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                BeforeDispose?.Invoke();
                _name.Dispose();
                if (!KeepStateAliveOnDispose)
                {
                    _state.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        protected override ValueTask DisposeAsyncCore()
        {
            BeforeDispose?.Invoke();
            _name.Dispose();
            if (!KeepStateAliveOnDispose)
            {
                _state.Dispose();
            }
            return base.DisposeAsyncCore();
        }
    }

    private sealed class AddressFactory
        : ClientDeviceFactory<ExampleMessage1, AddressDevice, AddressId>
    {
        public override int Order => Priority;
        public int Priority { get; init; }
        public string Family { get; init; } = "test";
        public string? SourceFamily { get; init; }
        public string Name { get; init; } = "default";
        public string? LocalFilter { get; init; }
        public bool DiscoveryHasSource { get; init; } = true;
        public bool ReturnDefaultAddress { get; init; }
        public int DiscoveryCalls { get; private set; }
        public int SourceCalls { get; private set; }
        public int DeviceAddressCalls { get; private set; }

        public override bool TryGetSourceAddress(
            IProtocolMessage message,
            out DeviceAddress address
        )
        {
            SourceCalls++;
            if (ReturnDefaultAddress)
            {
                address = default;
                return true;
            }
            if (
                message is ExampleMessageBase example
                && (DiscoveryHasSource || message is not ExampleMessage1)
            )
            {
                address = new DeviceAddress(
                    SourceFamily ?? Family,
                    example.SenderId.ToString(CultureInfo.InvariantCulture)
                );
                return true;
            }
            address = default;
            return false;
        }

        public override bool TryGetDeviceAddress(DeviceId deviceId, out DeviceAddress address)
        {
            DeviceAddressCalls++;
            if (ReturnDefaultAddress)
            {
                address = default;
                return true;
            }
            if (deviceId is AddressId id)
            {
                address = new DeviceAddress(
                    Family,
                    id.Remote.ToString(CultureInfo.InvariantCulture)
                );
                return true;
            }
            address = default;
            return false;
        }

        protected override bool InternalTryIdentify(ExampleMessage1 msg, out AddressId? deviceId)
        {
            DiscoveryCalls++;
            var local = msg.Tags["channel"] as string ?? "none";
            deviceId =
                LocalFilter is null || local == LocalFilter
                    ? new AddressId(msg.SenderId, local)
                    : null;
            return deviceId is not null;
        }

        protected override void InternalUpdateDevice(AddressDevice device, ExampleMessage1 msg)
        {
            device.Updates++;
            device.LastUpdater = Name;
        }

        protected override AddressDevice InternalCreateDevice(
            ExampleMessage1 msg,
            AddressId deviceId,
            IMicroserviceContext context,
            ImmutableArray<IClientDeviceExtender> extenders
        ) => new(deviceId);
    }

    private sealed class LegacyFactory : IClientDeviceFactory
    {
        public int Order => 0;

        public bool TryIdentify(IProtocolMessage message, out DeviceId? deviceId)
        {
            deviceId = message is ExampleMessage1 msg
                ? new AddressId(msg.SenderId, "legacy")
                : null;
            return deviceId is not null;
        }

        public void UpdateDevice(IClientDevice device, IProtocolMessage message) { }

        public IClientDevice CreateDevice(
            IProtocolMessage message,
            DeviceId deviceId,
            IMicroserviceContext context,
            ImmutableArray<IClientDeviceExtender> extenders
        ) => new AddressDevice(deviceId);
    }

    private sealed class LegacyTypedFactory<TMessage>
        : ClientDeviceFactory<TMessage, AddressDevice, AddressId>
        where TMessage : ExampleMessageBase
    {
        public override int Order => 0;

        protected override bool InternalTryIdentify(TMessage msg, out AddressId? deviceId)
        {
            deviceId = new AddressId(msg.SenderId, "shared");
            return true;
        }

        protected override void InternalUpdateDevice(AddressDevice device, TMessage msg)
        {
            device.Updates++;
            device.LastUpdater = msg.Name;
        }

        protected override AddressDevice InternalCreateDevice(
            TMessage msg,
            AddressId deviceId,
            IMicroserviceContext context,
            ImmutableArray<IClientDeviceExtender> extenders
        ) => new(deviceId);
    }

    private sealed class LegacyExplorer : IDeviceExplorer
    {
        public IReadOnlyObservableDictionary<DeviceId, IClientDevice> Devices { get; } =
            new ObservableDictionary<DeviceId, IClientDevice>();
        public IReadOnlyObservableList<IClientDevice> InitializedDevices { get; } =
            new ObservableList<IClientDevice>();

        public bool TryIdentify(
            IProtocolMessage message,
            [NotNullWhen(true)] out DeviceId? deviceId
        )
        {
            deviceId = null;
            return false;
        }

        public void Dispose() { }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
