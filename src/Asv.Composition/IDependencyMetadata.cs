namespace Asv.Composition;

public interface IDependencyMetadata
{
    string[] Dependencies { get; }
    string Name { get; }
}
