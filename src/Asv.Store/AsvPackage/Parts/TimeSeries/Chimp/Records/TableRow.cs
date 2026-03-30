using System;
using Asv.IO;

namespace Asv.Store;

public class TableRow(uint index, DateTime timestamp, string id, IVisitable data)
{
    public uint Index => index;
    public DateTime Timestamp => timestamp;
    public string Id => id;
    public IVisitable Data => data;
}
