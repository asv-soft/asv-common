namespace Asv.Modeling;

public interface INavigationStore
{
    void Load(Action<NavPath> addForward, Action<NavPath> addBackward);
    void Save(IEnumerable<NavPath> forward, IEnumerable<NavPath> backward);
}