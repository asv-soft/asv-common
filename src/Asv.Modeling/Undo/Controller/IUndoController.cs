namespace Asv.Modeling;

/// <summary>
/// Executes undo or redo logic for a typed change.
/// </summary>
/// <typeparam name="TChange">The concrete undo change type.</typeparam>
/// <param name="change">The change to apply or revert.</param>
/// <param name="cancel">A cancellation token for the operation.</param>
/// <returns>A task-like value that completes when the operation finishes.</returns>
public delegate ValueTask AsyncUndoCallback<in TChange>(TChange change, CancellationToken cancel)
    where TChange : IUndoChange;

/// <summary>
/// Executes synchronous undo or redo logic for a typed change.
/// </summary>
/// <typeparam name="TChange">The concrete undo change type.</typeparam>
/// <param name="change">The change to apply or revert.</param>
public delegate void UndoCallback<in TChange>(TChange change)
    where TChange : IUndoChange;

/// <summary>
/// Registers undoable model members and coordinates publication of their changes.
/// </summary>
public interface IUndoController
{
    /// <summary>
    /// Creates a registration for a typed undo change.
    /// </summary>
    /// <typeparam name="TChange">The concrete change type handled by the registration.</typeparam>
    /// <param name="registrationId">Unique identifier of the registration within this controller.</param>
    /// <param name="undo">Callback that reverts a change.</param>
    /// <param name="redo">Callback that reapplies a change.</param>
    /// <param name="factory">Factory used to create empty changes for history deserialization.</param>
    /// <returns>A sink used by the registered member to publish changes.</returns>
    IUndoChangeSink<TChange> Register<TChange>(
        string registrationId,
        AsyncUndoCallback<TChange> undo,
        AsyncUndoCallback<TChange> redo,
        Func<TChange> factory
    )
        where TChange : IUndoChange;

    /// <summary>
    /// Gets a registered change handler by its registration identifier.
    /// </summary>
    /// <param name="registrationId">The registration identifier.</param>
    /// <returns>The registered undo change handler.</returns>
    IUndoChangeHandler this[string registrationId] { get; }

    /// <summary>
    /// Temporarily suppresses publication of changes from this controller.
    /// </summary>
    /// <returns>
    /// A disposable scope. Disposing the returned object restores publication for this scope.
    /// </returns>
    IDisposable SuppressChangePublication();
}
