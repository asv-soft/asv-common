using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Asv.Common;
using R3;

namespace Asv.IO.Device;

public class ExampleDeviceConfig : ClientDeviceConfig
{
    public byte SelfId { get; set; } = 255;
    public double LinkTimeoutMs { get; set; } = 1000;
    public int DowngradeErrorCount { get; set; } = 3;
}

public class ExampleDevice : ClientDevice<ExampleDeviceId>
{
    private readonly ExampleDeviceId _id;
    private readonly byte _selfId;
    private readonly TimeBasedLinkIndicator _link;
    private readonly IDisposable _linkSubscription;
    public const string DeviceClass = "ExampleDevice";

    public ExampleDevice(
        ExampleDeviceId id,
        ExampleDeviceConfig config,
        ImmutableArray<IClientDeviceExtender> extenders,
        IMicroserviceContext context
    )
        : base(id, config, extenders, context)
    {
        _id = id;
        _selfId = config.SelfId;
        _link = new TimeBasedLinkIndicator(
            TimeSpan.FromMilliseconds(config.LinkTimeoutMs),
            config.DowngradeErrorCount,
            context.TimeProvider
        );
        _linkSubscription = context.Connection.OnRxMessage.Subscribe(_ => _link.Upgrade());
    }

    public override ILinkIndicator Link => _link;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _linkSubscription.Dispose();
            _link.Dispose();
        }

        base.Dispose(disposing);
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        _linkSubscription.Dispose();
        await _link.DisposeAsync().ConfigureAwait(false);
        await base.DisposeAsyncCore().ConfigureAwait(false);
    }

    protected override async IAsyncEnumerable<IMicroserviceClient> InternalCreateMicroservices(
        [EnumeratorCancellation] CancellationToken cancel
    )
    {
        var example = new ExampleDeviceMicroservice(
            $"{Id}.{ExampleDeviceMicroservice.MicroserviceType}",
            _id.TargetId,
            _selfId,
            Context
        );
        await example.Init(cancel).ConfigureAwait(false);
        yield return example;
    }
}
