namespace Asv.Modeling;

public interface INavigationRoot<TBase>
{
    INavigationController<TBase> Navigation { get; }
}