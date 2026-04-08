namespace Asv.Modeling;

public interface IUndoSnapshot<out TId>
{
    IEnumerable<TId> Path { get; }
    string ChangeId { get; }
    Ulid DataRefId { get; }
}
