namespace Asv.IO;

public static class ExampleProtocol
{
    public static ProtocolInfo Info = new("example","Example protocol");

    public static void RegisterExampleProtocol(this IProtocolBuilder builder)
    {
        builder.RegisterProtocol(Info, (core) => new ExampleParser(ExampleMessageFactory.Instance, core));
    }
}