using System.IO.Pipelines;
using DotNext.IO;

namespace Asv.Modeling;

public class UndoSnapshot<TId> : IUndoSnapshot<TId>
{
    public required IEnumerable<TId> Path { get; set; }
    public required string ChangeId { get; set; }
    public required Ulid DataRefId { get; set; }
    public byte[]? Data { get; set; }
}
