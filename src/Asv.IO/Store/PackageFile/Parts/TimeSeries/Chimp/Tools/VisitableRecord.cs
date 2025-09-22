using System;

namespace Asv.IO;

public class VisitableRecord(uint index, DateTime timestamp, string id, IVisitable data)
{
    public uint Index => index;
    public DateTime Timestamp => timestamp;
    public string Id => id;
    public IVisitable Data => data;
}
