using JetBrains.Annotations;


namespace Asv.IO.Test.Example.Protocol;

[TestSubject(typeof(ExampleMessage1))]
public class ExampleMessage1Test(ITestOutputHelper output)
    : ExampleMessageTestBase<ExampleMessage1>(output) { }
