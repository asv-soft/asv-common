namespace Asv.Modeling;

public interface ISupportRootTracking<TBase, TRoot>
    where TRoot : TBase
{
    IRootTrackingController<TRoot> RootTracking { get; }
}