namespace Asv.IO;


public class DeviceId(string id, DeviceClass @class)
{
    public string Id { get; } = id;
    public DeviceClass Class { get; } = @class;
}

public class DeviceClass(string title, string className)
{
    public string Title { get; } = title;
    public string ClassName { get; } = className;
}

