namespace Asv.Modeling;

public interface ILayoutStore : IDisposable
{
    bool Load(NavPath path, string layoutId, ILayoutData layoutData);
    void Save(NavPath path, string layoutId, ILayoutData layoutData);
}
