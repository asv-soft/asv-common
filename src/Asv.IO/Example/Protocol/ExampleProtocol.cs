using System;

namespace Asv.IO;

public static class ExampleProtocol
{
    public static readonly ProtocolInfo Info = new("example","Example protocol");

    public static void RegisterExampleProtocol(this IProtocolParserBuilder builder, Action<IProtocolMessageFactoryBuilder<ExampleMessageBase, byte>>? configure = null)
    {
        var factory = new ProtocolMessageFactoryBuilder<ExampleMessageBase, byte>(Info);
        factory
            .Add<ExampleMessage1>()
            .Add<ExampleMessage2>()
            .Add<ExampleMessage3>();
        configure?.Invoke(factory);
        var messageFactory = factory.Build();
        builder.Register(Info, (core,stat) => new ExampleParser(messageFactory, core,stat));
    }
}