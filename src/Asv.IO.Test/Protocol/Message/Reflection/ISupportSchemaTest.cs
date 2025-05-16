using System;
using JetBrains.Annotations;
using Xunit;

namespace Asv.IO.Test.Message.Reflection;


[TestSubject(typeof(IVisitable))]
public class SupportSchemaTest
{

    [Fact]
    public void METHOD()
    {
        var msg1 = new ExampleMessage1().Randomize();
        
    }
}