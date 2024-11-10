using System;
using System.Threading;
using System.Threading.Tasks;
using R3;

namespace Asv.IO;


public class VirtualDataStream : IDataStream, IDisposable,IAsyncDisposable
{
    private readonly string _name;
    private long _rxBytes;
    private long _txBytes;
    private readonly Subject<byte[]> _txPipe;
    private readonly Subject<byte[]> _rxPipe;
    private readonly IDisposable _sub1;

    public VirtualDataStream(string name)
    {
        _name = name;
        _txPipe = new Subject<byte[]>();
        _rxPipe = new Subject<byte[]>();
        _sub1 = _rxPipe.Subscribe(x => Interlocked.Add(ref _rxBytes, x.Length));
    }


    public Task<bool> Send(byte[] data, int count, CancellationToken cancel)
    {
        return Task.Run(() =>
        {
            Interlocked.Add(ref _txBytes, count);
            var dataToSend = new byte[count];
            Array.Copy(data, dataToSend, count);
            _txPipe.OnNext(dataToSend);
            return true;
        }, cancel);
    }

    public Task<bool> Send(ReadOnlyMemory<byte> data, CancellationToken cancel)
    {
        return Task.Run(() =>
        {
            Interlocked.Add(ref _txBytes, data.Length);
            var dataToSend = new byte[data.Length];
            data.CopyTo( dataToSend);
            _txPipe.OnNext(dataToSend);
            return true;
        }, cancel);
    }
    public Observer<byte[]> RxPipe => _rxPipe.AsObserver();
    public Observable<byte[]> TxPipe => _txPipe;
    public Observable<byte[]> OnReceive => _rxPipe;
    public string Name => _name;
    public long RxBytes => _rxBytes;
    public long TxBytes => _txBytes;

    #region Dispose

    public void Dispose()
    {
        _txPipe.Dispose();
        _rxPipe.Dispose();
        _sub1.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await CastAndDispose(_txPipe);
        await CastAndDispose(_rxPipe);
        await CastAndDispose(_sub1);

        return;

        static async ValueTask CastAndDispose(IDisposable resource)
        {
            if (resource is IAsyncDisposable resourceAsyncDisposable)
                await resourceAsyncDisposable.DisposeAsync();
            else
                resource.Dispose();
        }
    }

    #endregion
}