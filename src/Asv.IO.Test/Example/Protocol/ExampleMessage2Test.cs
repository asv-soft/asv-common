using JetBrains.Annotations;
using Xunit.Abstractions;

namespace Asv.IO.Test.Example.Protocol;

[TestSubject(typeof(ExampleMessage1))]
public class ExampleMessage2Test(ITestOutputHelper output)  
    : ExampleMessageTestBase<ExampleMessage2>(output)
{

}