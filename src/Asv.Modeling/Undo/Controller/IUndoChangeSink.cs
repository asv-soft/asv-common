namespace Asv.Modeling;

/// <summary>
/// Publishes changes from a model member into an undo controller registration.
/// </summary>
/// <typeparam name="TChange">The type of change accepted by this sink.</typeparam>
public interface IUndoChangeSink<in TChange> : IDisposable
    where TChange : IUndoChange
{
    /// <summary>
    /// Temporarily suppresses publication of changes from this sink.
    /// </summary>
    /// <returns>
    /// A disposable scope. Disposing the returned object restores publication for this scope.
    /// </returns>
    IDisposable SuppressChangePublication();

    /// <summary>
    /// Publishes a change to the owning undo controller.
    /// </summary>
    /// <param name="change">The change to publish.</param>
    void Publish(TChange change);
}
