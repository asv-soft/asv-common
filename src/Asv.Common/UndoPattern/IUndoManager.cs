using System;
using System.Threading;
using System.Threading.Tasks;
using R3;

namespace Asv.Common;

/// <summary>
/// History stack manager for commands. It allows to undo and redo commands.
/// </summary>
public interface IUndoManager
{
    ReadOnlyReactiveProperty<bool> CanUndo { get; }
    ReadOnlyReactiveProperty<bool> CanRedo { get; }
    IUndoTransaction CreateTransaction(string displayName);
}

/// <summary>
/// Transaction for undo/redo operations.
/// It allows grouping multiple commands into a single transaction.
/// When the transaction is committed, all commands in the transaction are executed.
/// </summary>
public interface IUndoTransaction : IDisposable, IAsyncDisposable
{
    string DisplayName { get; }
    void Add(IUndoOperation command);
    ValueTask Commit(CancellationToken cancel = default);
}

public interface IUndoOperation
{
    string DisplayName { get; }
    ValueTask Execute(CancellationToken cancel);
    ValueTask Undo(CancellationToken cancel);
}

public abstract class UndoOperation : IUndoOperation
{
    public abstract string DisplayName { get; }
    public abstract ValueTask Execute(CancellationToken cancel);
    public abstract ValueTask Undo(CancellationToken cancel);
}

public class UndoCollectionAddOperation : UndoOperation
{
    public override string DisplayName => "Create";

    public override ValueTask Execute(CancellationToken cancel) =>
        throw new NotImplementedException();

    public override ValueTask Undo(CancellationToken cancel) => throw new NotImplementedException();
}
