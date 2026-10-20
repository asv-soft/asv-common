using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Time.Testing;
using R3;
using Xunit;

namespace Asv.IO.Test;

public class TransportLifetimeTest
{
    [Fact]
    public async Task LegacyPort_RepeatedErrorsKeepOnlyLatestReconnectSchedule()
    {
        var time = new FakeTimeProvider();
        using var port = new CountingPort(time);
        await EnableAndWaitForStart(port);
        port.Fail();
        time.Advance(TimeSpan.FromSeconds(1));
        port.Fail();

        time.Advance(TimeSpan.FromSeconds(4));
        Assert.Equal(1, port.Starts);
        time.Advance(TimeSpan.FromSeconds(1));
        Assert.Equal(2, port.Starts);
        Assert.Equal(PortState.Connected, port.State.CurrentValue);
        time.Advance(TimeSpan.FromSeconds(10));
        Assert.Equal(2, port.Starts);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LegacyPort_StopCancelsScheduledReconnect(bool dispose)
    {
        var time = new FakeTimeProvider();
        using var port = new CountingPort(time);
        await EnableAndWaitForStart(port);
        port.Fail();

        if (dispose)
        {
            port.Dispose();
        }
        else
        {
            port.Disable();
        }
        time.Advance(TimeSpan.FromSeconds(10));

        Assert.Equal(1, port.Starts);
    }

    [Fact]
    public async Task TcpWriter_SlowRecipientHasBoundedQueueAndStopsOnDispose()
    {
        await using var stream = new GatedWriteStream();
        using var writer = new TcpClientSendQueue(stream, () => { }, capacity: 2);
        Assert.True(writer.TrySend(new byte[] { 1 }));
        await Wait(stream.Started.Task);

        Assert.True(writer.TrySend(new byte[] { 2 }));
        Assert.True(writer.TrySend(new byte[] { 3 }));
        Assert.False(writer.TrySend(new byte[] { 4 }));
        writer.Dispose();
        await Wait(writer.Completion);

        Assert.Single(stream.Writes);
        Assert.True(stream.ObservedCancel.IsCancellationRequested);
    }

    [Fact]
    public async Task TcpWriter_QueuedSendsOwnTheirBytesAndPreserveOrder()
    {
        await using var stream = new GatedWriteStream();
        using var writer = new TcpClientSendQueue(stream, () => { }, capacity: 3);
        var first = new byte[] { 1, 2 };
        var second = new byte[] { 3, 4 };
        var sentFirst = writer.SendAsync(first, CancellationToken.None);
        await Wait(stream.Started.Task);
        var sentSecond = writer.SendAsync(second, CancellationToken.None);
        first[0] = 99;
        second[0] = 99;
        Assert.False(sentFirst.IsCompleted);
        Assert.False(sentSecond.IsCompleted);

        stream.Release.TrySetResult();
        await Wait(Task.WhenAll(sentFirst, sentSecond));

        Assert.Equal(2, stream.Writes.Count);
        Assert.Equal(new byte[] { 1, 2 }, stream.Writes[0]);
        Assert.Equal(new byte[] { 3, 4 }, stream.Writes[1]);
        writer.Dispose();
        await Wait(writer.Completion);
    }

    [Fact]
    public async Task LegacyTcpServer_GracefulDisconnectRemovesAndDisposesAcceptedClient()
    {
        await using var server = new TcpServerPort(
            new TcpPortConfig
            {
                Host = "127.0.0.1",
                Port = 0,
                IsServer = true,
            }
        );
        var accepted = server.OnTcpClientAdded.FirstAsync();
        var removed = server.OnTcpClientRemoved.FirstAsync();
        var connected = server.State.FirstAsync(state => state == PortState.Connected);
        server.Enable();
        await Wait(connected);
        var portNumber = Assert.IsType<IPEndPoint>(server.LocalEndPoint).Port;
        using var remote = new TcpClient();
        await remote.ConnectAsync(
            IPAddress.Loopback,
            portNumber,
            TestContext.Current.CancellationToken
        );
        var acceptedClient = await accepted.WaitAsync(
            TimeSpan.FromSeconds(5),
            TestContext.Current.CancellationToken
        );

        remote.Client.Shutdown(SocketShutdown.Both);
        remote.Close();
        var removedClient = await removed.WaitAsync(
            TimeSpan.FromSeconds(5),
            TestContext.Current.CancellationToken
        );

        Assert.Same(acceptedClient, removedClient);
        Assert.True(acceptedClient.Client == null || acceptedClient.Client.SafeHandle.IsClosed);
        Assert.Contains("Clients [0]", server.ToString());
    }

    private static Task Wait(Task task) =>
        task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

    private static async Task EnableAndWaitForStart(CountingPort port)
    {
        // Enable schedules the initial connection on the current scheduler. Drain it
        // before advancing virtual time so reconnect tests do not race that worker.
        var scheduler = new ConcurrentExclusiveSchedulerPair(TaskScheduler.Default, 1);
        await Wait(
            Task.Factory.StartNew(
                port.Enable,
                TestContext.Current.CancellationToken,
                TaskCreationOptions.None,
                scheduler.ExclusiveScheduler
            )
        );
        scheduler.Complete();
        await Wait(scheduler.Completion);
        Assert.Equal(PortState.Connected, port.State.CurrentValue);
    }

    private sealed class CountingPort(TimeProvider time) : PortBase(time, null)
    {
        private int _starts;
        public int Starts => Volatile.Read(ref _starts);
        public override PortType PortType => PortType.Tcp;
        public override string PortLogName => nameof(CountingPort);

        public void Fail() => InternalOnError(new IOException("Test failure."));

        protected override void InternalStart() => Interlocked.Increment(ref _starts);

        protected override void InternalStop() { }

        protected override Task InternalSend(ReadOnlyMemory<byte> data, CancellationToken cancel) =>
            Task.CompletedTask;

        protected override Task InternalSend(byte[] data, int count, CancellationToken cancel) =>
            Task.CompletedTask;
    }

    private sealed class GatedWriteStream : Stream
    {
        public TaskCompletionSource Started { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        public List<byte[]> Writes { get; } = [];
        public CancellationToken ObservedCancel { get; private set; }
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush() { }

        public override int Read(byte[] buffer, int offset, int count) =>
            throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) =>
            throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) =>
            throw new NotSupportedException();

        public override async ValueTask WriteAsync(
            ReadOnlyMemory<byte> buffer,
            CancellationToken cancellationToken = default
        )
        {
            Writes.Add(buffer.ToArray());
            ObservedCancel = cancellationToken;
            Started.TrySetResult();
            await Release.Task.WaitAsync(cancellationToken);
        }
    }
}
