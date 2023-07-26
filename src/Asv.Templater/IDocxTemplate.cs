namespace Asv.Templater;

public interface IDocxTemplate : IDisposable
{
    void Image(string tag, string path, double width, double height);
    void Image(string tag, MemoryStream imageStream, double width, double height);
    bool Tag(string tagName, string value);
    void Save(string filePath);
    void FixedTable(string column, IEnumerable<string> data);
    void DynamicTable(string column, IEnumerable<string> data);
}