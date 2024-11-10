using System;
using System.Threading;
using System.Threading.Tasks;
using Asv.Common;
using R3;

namespace Asv.IO;


public class VirtualDataStream : IDataStream
{
    private readonly string _name;
    private long _rxBytes;
    private long _txBytes;
    private readonly Subject<byte[]> _txPipe;
    private readonly Subject<byte[]> _rxPipe;

    public VirtualDataStream(string name)
    {
        _name = name;
        _txPipe = new Subject<byte[]>();
        _rxPipe = new Subject<byte[]>();
        _rxPipe.Subscribe(x => Interlocked.Add(ref _rxBytes, x.Length));
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

    public string Name => _name;

    public long RxBytes => _rxBytes;

    public long TxBytes => _txBytes;
}