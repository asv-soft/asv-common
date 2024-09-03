using System.Composition;

namespace Asv.Composition;

/// <summary>
/// Define this attribute to export class with dependencies
/// </summary>
[MetadataAttribute]
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class ExportDependencyAttribute : ExportAttribute,IDependencyMetadata
{
    public ExportDependencyAttribute(Type type, string name,params string[] dependency)
        : base(type)
    {
        if (string.IsNullOrWhiteSpace(name))
            ArgumentException.ThrowIfNullOrWhiteSpace(nameof(name));
        Name = name;
        Dependencies = dependency;
    }
    public string[] Dependencies { get; }
    public string Name { get; }
}