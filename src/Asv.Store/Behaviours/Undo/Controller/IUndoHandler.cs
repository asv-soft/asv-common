using System.Threading;
using System.Threading.Tasks;
using R3;

namespace Asv.Common;

public interface IUndoHandler
{
    string RegistrationId { get; }
    Observable<IChange> Changes { get; }
    IChange Create();
    ValueTask Undo(IChange change, CancellationToken cancel);
    ValueTask Redo(IChange change, CancellationToken cancel);
}
