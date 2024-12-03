namespace Asv.IO.Device;

public class ExampleDeviceMicroservice:MicroserviceClient<ExampleMessageBase>
{
    private readonly byte _selfId;
    private readonly byte _targetId;
    public const string MicroserviceType = "ExampleMicroservice";
    
    public ExampleDeviceMicroservice(string id, byte selfId, byte targetId, IDeviceContext context) 
        : base(context, id)
    {
        _selfId = selfId;
        _targetId = targetId;
    }


    public override string Type => MicroserviceType;
    protected override void FillMessageBeforeSent(ExampleMessageBase message)
    {
        message.SenderId = _selfId;
    }

    protected override bool FilterDeviceMessages(ExampleMessageBase arg)
    {
        return arg.SenderId == _targetId;
    }
}