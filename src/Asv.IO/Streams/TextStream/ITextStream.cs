using System;
using System.Threading;
using System.Threading.Tasks;
using R3;

namespace Asv.IO
{
    public interface ITextStream : IDisposable, IAsyncDisposable
    {
        Observable<string> OnReceive { get; }
        Observable<Exception> OnError { get; }
        Task Send(string value, CancellationToken cancel);
    }


    public static class TextStreamHelper
    {
        public static async Task<string> RequestText(this ITextStream strm,string request, int timeoutMs, CancellationToken cancel)
        {
            using var linkedCancel = CancellationTokenSource.CreateLinkedTokenSource(cancel);
            linkedCancel.CancelAfter(timeoutMs);
            var tcs = new TaskCompletionSource<string>();
            using var c1 = linkedCancel.Token.Register(tcs.SetCanceled);
            using var subscribe = strm.OnReceive.Take(1).Subscribe(tcs.SetResult);
            await strm.Send(request, linkedCancel.Token);
            return await tcs.Task.ConfigureAwait(false);
        }
    }
}
