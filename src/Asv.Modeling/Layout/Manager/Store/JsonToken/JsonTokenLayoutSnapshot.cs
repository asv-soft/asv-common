using Newtonsoft.Json.Linq;

namespace Asv.Modeling;

public sealed class JsonTokenLayoutSnapshot
{
    public required string Path { get; set; }
    public required string LayoutId { get; set; }
    public required JToken Data { get; set; }
}
