using ObservableCollections;
using R3;

namespace Asv.Modeling;

/// <summary>
/// Maintains undo and redo stacks and executes history operations for a tree.
/// </summary>
/// <typeparam name="TBase">The tree node type.</typeparam>
public interface IUndoHistory<TBase> : IDisposable
    where TBase : ISupportRoutedEvents<TBase>, ISupportNavigation<TBase>
{
    /// <summary>
    /// Gets the command that executes <see cref="UndoAsync"/>.
    /// </summary>
    ReactiveCommand Undo { get; }

    /// <summary>
    /// Executes the latest undo operation.
    /// </summary>
    /// <param name="cancel">A cancellation token for the undo operation.</param>
    /// <returns>A task-like value that completes when the operation finishes.</returns>
    ValueTask UndoAsync(CancellationToken cancel = default);

    /// <summary>
    /// Gets the current undo stack.
    /// </summary>
    IObservableCollection<IUndoSnapshot> UndoStack { get; }

    /// <summary>
    /// Gets the command that executes <see cref="RedoAsync"/>.
    /// </summary>
    ReactiveCommand Redo { get; }

    /// <summary>
    /// Executes the latest redo operation.
    /// </summary>
    /// <param name="cancel">A cancellation token for the redo operation.</param>
    /// <returns>A task-like value that completes when the operation finishes.</returns>
    ValueTask RedoAsync(CancellationToken cancel = default);

    /// <summary>
    /// Gets the current redo stack.
    /// </summary>
    IObservableCollection<IUndoSnapshot> RedoStack { get; }
}
