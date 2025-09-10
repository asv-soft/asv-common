namespace Asv.Common;

public class ManualLinkIndicator(int downgradeErrors = 3) : LinkIndicatorBase(downgradeErrors)
{
    public void Upgrade()
    {
        InternalUpgrade();
    }

    public void Downgrade()
    {
        InternalDowngrade();
    }
}
