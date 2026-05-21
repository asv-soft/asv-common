using Newtonsoft.Json.Linq;

namespace Asv.Modeling;

/// <summary>
/// Represents a serialized layout value stored in a JSON layout file.
/// </summary>
public sealed class JsonTokenLayoutSnapshot
{
    /// <summary>
    /// Gets or sets the navigation path of the object that owns the layout value.
    /// </summary>
    public required string Path { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the layout value within the owner.
    /// </summary>
    public required string LayoutId { get; set; }

    /// <summary>
    /// Gets or sets the serialized layout value.
    /// </summary>
    public required JToken Data { get; set; }
}
