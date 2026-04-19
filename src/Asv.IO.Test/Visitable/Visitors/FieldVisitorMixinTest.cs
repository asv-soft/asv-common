using JetBrains.Annotations;
using Xunit;

namespace Asv.IO.Test.Visitable.Visitors;

[TestSubject(typeof(FieldVisitorMixin))]
public class FieldVisitorMixinTest(ITestOutputHelper output)
{
    [Fact]
    public void PrintValues_RandomizedMessage_WritesOutputSuccessfully()
    {
        output.WriteLine(new AllTypesStruct().Randomize().PrintValues());
    }
}
