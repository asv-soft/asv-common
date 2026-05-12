namespace Asv.Modeling;

/// <summary>
/// Publishes changes from a model member into an undo controller registration.
/// </summary>
/// <typeparam name="T">The type of change accepted by this sink.</typeparam>
public interface IUndoChangeSink<in T> : IDisposable
    where T : IUndoChange
{
    /// <summary>
    /// Publishes a change to the owning undo controller.
    /// </summary>
    /// <param name="change">The change to publish.</param>
    void Publish(T change);
}
