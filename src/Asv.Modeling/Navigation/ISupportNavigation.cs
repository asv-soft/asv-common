namespace Asv.Modeling;

public interface ISupportNavigation<TBase> : ISupportId<NavId>
{
    ValueTask<TBase> Navigate(NavId id);
}