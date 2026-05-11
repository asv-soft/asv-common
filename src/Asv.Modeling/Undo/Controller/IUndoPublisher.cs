namespace Asv.Modeling;

public interface IUndoPublisher<in T> : IDisposable
    where T : IChange
{
    void Publish(T change);
}
