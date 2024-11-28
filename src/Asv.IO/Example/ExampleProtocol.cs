namespace Asv.IO;

public static class ExampleProtocol
{
    public static readonly ProtocolInfo Info = new("example","Example protocol");

    public static void RegisterExampleProtocol(this IProtocolBuilder builder)
    {
        builder.RegisterProtocol(Info, (core,stat) => new ExampleParser(ExampleMessageFactory.Instance, core,stat));
    }
}