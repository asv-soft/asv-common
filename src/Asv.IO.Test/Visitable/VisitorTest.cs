using JetBrains.Annotations;
using Xunit;

namespace Asv.IO.Test.Visitable;

[TestSubject(typeof(IVisitor))]
public class VisitorTest
{

    [Fact]
    public void METHOD()
    {
        var msg1 = new ExampleMessage1().Randomize();
        
    }
}