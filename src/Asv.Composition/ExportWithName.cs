using System.Composition;

namespace Asv.Composition;

[MetadataAttribute]
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class ExportWithName : ExportAttribute,INameMetadata
{
    public ExportWithName(Type type, string name, int priority)
        :base(type)
    {
        if (string.IsNullOrWhiteSpace(name))
            ArgumentException.ThrowIfNullOrWhiteSpace(nameof(name));
        Name = name;
        Priority = priority;
    }

    public string Name { get; }
    public int Priority { get; }
}