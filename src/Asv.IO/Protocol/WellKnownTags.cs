using System.Diagnostics;

namespace Asv.IO;

public static class WellKnownTags
{
    public const string PortIdTagName = "PortId";

    public static void SetPortId(ref TagList tagList, string id)
    {
        tagList.Add(PortIdTagName,id);
    }
}