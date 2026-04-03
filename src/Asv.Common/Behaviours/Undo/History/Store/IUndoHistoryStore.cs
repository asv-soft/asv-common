using System;
using System.Buffers;
using System.Collections.Generic;

namespace Asv.Common;

public interface IUndoHistoryStore<TId> : IDisposable
{
    IEnumerable<IUndoSnapshot<TId>> LoadUndoStack();
    void SaveUndoStack(IEnumerable<IUndoSnapshot<TId>> snapshots);

    IEnumerable<IUndoSnapshot<TId>> LoadRedoStack();
    void SaveRedoStack(IEnumerable<IUndoSnapshot<TId>> snapshots);
    void LoadChange(Guid snapshotDataId, IChange item);
    IUndoSnapshot<TId> CreateSnapshot(IEnumerable<TId> path, string changeId);
    void SaveChange(IChange change);
}
