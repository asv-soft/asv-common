namespace Asv.Modeling;

public interface ISupportChildren<out T>
    where T : ISupportChildren<T>
{
    IEnumerable<T> GetChildren();
}
