namespace Asv.IO;

public static class WellKnownTags
{
    public const string PortIdTag = "PortId";
    public const string PortNameTag = "PortName";

    public static string? GetPortId(this ISupportTag src)
    {
        return (string?)src.Tags[PortIdTag];
    }

    public static void SetPortId(this ISupportTag src, string id)
    {
        src.Tags[PortIdTag] = id;
    }

    public static void SetPortName(this ISupportTag src, string name)
    {
        src.Tags[PortNameTag] = name;
    }
}
