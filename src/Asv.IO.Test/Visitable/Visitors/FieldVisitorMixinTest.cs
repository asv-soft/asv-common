using Asv.IO;
using JetBrains.Annotations;
using Xunit;
using Xunit.Abstractions;

namespace Asv.IO.Test.Visitable.Visitors;

[TestSubject(typeof(FieldVisitorMixin))]
public class FieldVisitorMixinTest(ITestOutputHelper output)
{

    [Fact]
    public void PrintValues_RandomizedMessage_WritesOutputSuccessfully()
    {
        output.WriteLine(new ExampleMessage1().Randomize().PrintValues());
        
    }
}