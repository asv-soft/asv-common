using System;
using System.Collections.Immutable;
using System.Diagnostics.Metrics;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using R3;

namespace Asv.IO.Test.Encoder;

internal static class EncoderTestSupport
{
    public static readonly ProtocolInfo Info = new("encoder-test", "Encoder test");

    public static IProtocolContext CreateContext(
        EncoderFactoryDelegate? encoderFactory = null,
        IProtocolFeature? feature = null
    )
    {
        var factory = Protocol.Create(builder =>
        {
            builder.Protocols.Register(Info, (_, _) => new TestParser());
            if (encoderFactory != null)
            {
                builder.Protocols.RegisterEncoder(Info, encoderFactory);
            }
            if (feature != null)
            {
                builder.Features.Register(feature);
            }
        });
        return (IProtocolContext)factory;
    }

    public static Task<T> Wait<T>(Task<T> task) =>
        task.WaitAsync(TimeSpan.FromSeconds(10), Xunit.TestContext.Current.CancellationToken);

    public static Task Wait(Task task) =>
        task.WaitAsync(TimeSpan.FromSeconds(10), Xunit.TestContext.Current.CancellationToken);

    public sealed class TestMessage : IProtocolMessage
    {
        private ProtocolTags _tags = [];

        public TestMessage() { }

        public TestMessage(byte value, int maximumSize = 1)
        {
            Payload = [value];
            MaximumSize = maximumSize;
        }

        public byte[] Payload { get; set; } = [0];
        public int MaximumSize { get; set; } = 1;
        public int SerializeCalls { get; private set; }
        public ProtocolInfo Protocol { get; set; } = Info;
        public string Name => nameof(TestMessage);
        public ref ProtocolTags Tags => ref _tags;

        public string GetIdAsString() => "1";

        public int GetByteSize() => MaximumSize;

        public void Serialize(ref Span<byte> buffer)
        {
            SerializeCalls++;
            Payload.CopyTo(buffer);
            buffer = buffer[Payload.Length..];
        }

        public void Deserialize(ref ReadOnlySpan<byte> buffer)
        {
            Payload = buffer[..MaximumSize].ToArray();
            buffer = buffer[MaximumSize..];
        }
    }

    public sealed class StatefulEncoder : IProtocolEncoder
    {
        public int EncodeCalls { get; private set; }

        public int GetMaxByteSize(IProtocolMessage message) => 8;

        public int Encode(IProtocolMessage message, Span<byte> destination)
        {
            destination[0] = (byte)EncodeCalls;
            destination[1] = ((TestMessage)message).Payload[0];
            EncodeCalls++;
            return 2;
        }
    }

    public sealed class TestParser : IProtocolParser
    {
        private readonly Subject<IProtocolMessage> _messages = new();
        private readonly Subject<Exception> _errors = new();

        public IStatistic Statistic { get; } = new Statistic();
        public ProtocolInfo Info => EncoderTestSupport.Info;
        public ProtocolTags Tags { get; } = [];
        public Observable<IProtocolMessage> OnMessage => _messages;
        public Observable<Exception> OnError => _errors;

        public bool Push(byte data) => false;

        public void Reset() { }

        public void Dispose()
        {
            _messages.Dispose();
            _errors.Dispose();
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }

    public sealed class TestFeature(Func<IProtocolMessage, ValueTask<IProtocolMessage?>> filter)
        : IProtocolFeature
    {
        public string Name => nameof(TestFeature);
        public string Description => Name;
        public string Id => Name;
        public int Priority => 0;

        public ValueTask<IProtocolMessage?> ProcessTx(
            IProtocolMessage message,
            IProtocolConnection connection,
            CancellationToken cancel
        ) => filter(message);

        public ValueTask<IProtocolMessage?> ProcessRx(
            IProtocolMessage message,
            IProtocolConnection connection,
            CancellationToken cancel
        ) => ValueTask.FromResult<IProtocolMessage?>(message);

        public void Register(IProtocolConnection connection) { }

        public void Unregister(IProtocolConnection connection) { }
    }

    public sealed class CapturingEndpoint : ProtocolEndpoint
    {
        private readonly Channel<WriteAttempt> _writes = Channel.CreateUnbounded<WriteAttempt>();
        private readonly Channel<IProtocolMessage> _published =
            Channel.CreateUnbounded<IProtocolMessage>();
        private int _writeCalls;

        public CapturingEndpoint(IProtocolContext context, ProtocolPortConfig? config = null)
            : base(
                Guid.NewGuid().ToString(),
                config ?? new ProtocolPortConfig(new Uri("test://localhost")),
                [new TestParser()],
                context
            )
        {
            OnTxMessage.Subscribe(message => _published.Writer.TryWrite(message));
        }

        public int WriteCalls => Volatile.Read(ref _writeCalls);

        public Task<WriteAttempt> ReadWrite() => Wait(_writes.Reader.ReadAsync().AsTask());

        public Task<IProtocolMessage> ReadPublished() =>
            Wait(_published.Reader.ReadAsync().AsTask());

        protected override int GetAvailableBytesToRead() => 1;

        protected override async ValueTask<int> InternalRead(
            Memory<byte> memory,
            CancellationToken cancel
        )
        {
            return await new TaskCompletionSource<int>(
                TaskCreationOptions.RunContinuationsAsynchronously
            ).Task.WaitAsync(cancel);
        }

        protected override async ValueTask<int> InternalWrite(
            ReadOnlyMemory<byte> memory,
            CancellationToken cancel
        )
        {
            Interlocked.Increment(ref _writeCalls);
            var attempt = new WriteAttempt(memory.ToArray());
            await _writes.Writer.WriteAsync(attempt, cancel);
            return await attempt.Completion.Task.WaitAsync(cancel);
        }
    }

    public sealed class WriteAttempt(byte[] bytes)
    {
        public byte[] Bytes { get; } = bytes;
        public TaskCompletionSource<int> Completion { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void Complete() => Completion.SetResult(Bytes.Length);
    }

    // Deliberately implements only the pre-encoder public context contract.
    public sealed class LegacyContext(IProtocolContext inner) : IProtocolContext
    {
        public ILoggerFactory LoggerFactory => inner.LoggerFactory;
        public TimeProvider TimeProvider => inner.TimeProvider;
        public IMeterFactory MeterFactory => inner.MeterFactory;
        public ImmutableDictionary<string, ParserFactoryDelegate> ParserFactory =>
            inner.ParserFactory;
        public ImmutableArray<IProtocolFeature> Features => inner.Features;
        public ImmutableArray<ProtocolInfo> AvailableProtocols => inner.AvailableProtocols;
        public ImmutableDictionary<string, PortFactoryDelegate> PortFactory => inner.PortFactory;
        public ImmutableArray<PortTypeInfo> AvailablePortTypes => inner.AvailablePortTypes;
        public ImmutableArray<IProtocolMessageFormatter> Formatters => inner.Formatters;

        public string? PrintMessage(
            IProtocolMessage message,
            PacketFormatting formatting = PacketFormatting.Inline
        ) => inner.PrintMessage(message, formatting);
    }
}
