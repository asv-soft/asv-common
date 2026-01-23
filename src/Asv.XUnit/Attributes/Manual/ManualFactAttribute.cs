using Xunit;

namespace Asv.XUnit;

public sealed class ManualFactAttribute : FactAttribute
{
    public ManualFactAttribute()
    {
        Skip = ManualAttributeHelper.SkipMessage;
    }
}
