namespace Asv.Common;

public interface ISupportParent<T>
    where T : ISupportParent<T>
{
    T? Parent { get; set; }
}
