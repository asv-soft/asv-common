namespace Asv.Composition;

public class DependencyMetadata : IDependencyMetadata
{
    public string[] Dependencies { get; set; } = null!;
    public string Name { get; set; } = null!;
}