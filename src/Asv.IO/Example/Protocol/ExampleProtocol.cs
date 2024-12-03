namespace Asv.IO;

public static class ExampleProtocol
{
    public static readonly ProtocolInfo Info = new("example","Example protocol");

    public static void RegisterExampleProtocol(this IProtocolParserBuilder builder)
    {
        builder.Register(Info, (core,stat) => new ExampleParser(ExampleMessageFactory.Instance, core,stat));
    }
}