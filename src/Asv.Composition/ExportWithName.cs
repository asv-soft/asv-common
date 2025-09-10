using System.Composition;

namespace Asv.Composition;

[MetadataAttribute]
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class ExportWithNameAttribute : ExportAttribute, INameMetadata
{
    public ExportWithNameAttribute(Type type, string name, int priority)
        : base(type)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(nameof(name));
        }

        Name = name;
        Priority = priority;
    }

    public string Name { get; }
    public int Priority { get; }
}

[MetadataAttribute]
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class ExportWithNameAsContractAttribute : ExportAttribute, INameMetadata
{
    public ExportWithNameAsContractAttribute(Type type, string name, int priority)
        : base(name, type)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(nameof(name));
        }

        Name = name;
        Priority = priority;
    }

    public string Name { get; }
    public int Priority { get; }
}
