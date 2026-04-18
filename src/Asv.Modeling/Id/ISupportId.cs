namespace Asv.Modeling;

public interface ISupportId<out TId>
{
    TId Id { get; }
}