namespace Asv.Modeling;

public class JsonLayoutSnapshot
{
    public required string Path { get; set; }
    public required string LayoutId { get; set; }
    public required int SchemaVersion { get; set; }
    public required string Json { get; set; }
}
