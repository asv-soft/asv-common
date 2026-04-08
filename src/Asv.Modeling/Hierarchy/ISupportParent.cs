namespace Asv.Modeling;

public interface ISupportParent<T>
    where T : ISupportParent<T>
{
    T? Parent { get; set; }
}
