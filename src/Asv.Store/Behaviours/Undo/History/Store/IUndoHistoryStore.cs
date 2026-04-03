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

public class UndoHistoryStore<TId> : AsyncDisposableOnceBag, IUndoHistoryStore<TId>
{
    public IEnumerable<IUndoSnapshot<TId>> LoadUndoStack()
    {
        yield break;
    }

    public void SaveUndoStack(IEnumerable<IUndoSnapshot<TId>> snapshots)
    {
        
    }

    public IEnumerable<IUndoSnapshot<TId>> LoadRedoStack()
    {
        throw new NotImplementedException();
    }

    public void SaveRedoStack(IEnumerable<IUndoSnapshot<TId>> snapshots)
    {
        throw new NotImplementedException();
    }

    public void LoadChange(Guid snapshotDataId, IChange item)
    {
        throw new NotImplementedException();
    }

    public IUndoSnapshot<TId> CreateSnapshot(IEnumerable<TId> path, string changeId)
    {
        throw new NotImplementedException();
    }

    public void SaveChange(IChange change)
    {
        throw new NotImplementedException();
    }
}
