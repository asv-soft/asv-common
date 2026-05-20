using System.Text.Json.Serialization.Metadata;

namespace Asv.Modeling;

/// <summary>
/// Represents display state that can be persisted by the layout store.
/// </summary>
public interface ILayoutData
{
    int SchemaVersion => 1;
}

/// <summary>
/// Represents display state with source-generated JSON serialization metadata.
/// </summary>
/// <typeparam name="TSelf">The concrete layout data type.</typeparam>
public interface IJsonLayoutData<TSelf> : ILayoutData
    where TSelf : IJsonLayoutData<TSelf>
{
    static abstract JsonTypeInfo<TSelf> JsonTypeInfo { get; }
}

/// <summary>
/// Represents display state that can copy data from another instance of the same type.
/// </summary>
/// <typeparam name="TSelf">The concrete layout data type.</typeparam>
public interface IMutableLayoutData<TSelf> : IJsonLayoutData<TSelf>
    where TSelf : IMutableLayoutData<TSelf>
{
    void CopyFrom(TSelf source);
}
