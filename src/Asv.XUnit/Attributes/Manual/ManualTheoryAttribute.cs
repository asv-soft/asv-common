using Xunit;

namespace Asv.XUnit;

public sealed class ManualTheoryAttribute : TheoryAttribute
{
    public ManualTheoryAttribute()
    {
        Skip = ManualAttributeHelper.SkipMessage;
    }
}
