namespace Asv.Modeling;

public interface IUndoSnapshot
{
    NavPath Path { get; }
    string ChangeId { get; }
    Ulid DataRefId { get; }
}
