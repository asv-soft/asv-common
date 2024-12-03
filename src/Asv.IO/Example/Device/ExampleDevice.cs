using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading;
using Asv.Common;

namespace Asv.IO.Device;

public class ExampleDeviceConfig:ClientDeviceConfig
{
    public byte SelfId { get; set; } = 255;
    public double LinkTimeoutMs { get; set; } = 1000;
}
public class ExampleDevice:ClientDevice<ExampleDeviceId>
{
    private readonly ExampleDeviceId _id;
    private readonly byte _selfId;
    private readonly TimeBasedLinkIndicator _link;
    public const string DeviceClass = "Example";
    public ExampleDevice(ExampleDeviceId id, ExampleDeviceConfig config, ImmutableArray<IClientDeviceExtender> extenders, IDeviceContext context) 
        : base(id, config, extenders, context)
    {
        _id = id;
        _selfId = config.SelfId;
        _link = new TimeBasedLinkIndicator(TimeSpan.FromMilliseconds(config.LinkTimeoutMs),3,context.TimeProvider);
    }

    public override ILinkIndicator Link => _link;

    protected override async IAsyncEnumerable<IMicroserviceClient> InternalCreateMicroservices([EnumeratorCancellation] CancellationToken cancel)
    {
        var example =
            new ExampleDeviceMicroservice($"{Id}.{ExampleDeviceMicroservice.MicroserviceType}", _id.TargetId,_selfId, Context);
        await example.Init(cancel);
        yield return example;
    }
}